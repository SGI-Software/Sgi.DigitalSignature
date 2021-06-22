using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Signatures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using Sgi.DigitalSignature.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sgi.DigitalSignature.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssinarController : ControllerBase
    {
        private readonly Guid _requestId = Guid.NewGuid();
        private const string PASTA_ARQUIVO_ASSINADO = "resultado/";
        private readonly DirectoryInfo _diretorioArquivoRecebido = new DirectoryInfo(Directory.GetCurrentDirectory() + "/wwwroot/tmp/");
        private readonly DirectoryInfo _diretorioArquivoAssinado = new DirectoryInfo(Directory.GetCurrentDirectory() + "/wwwroot/" + PASTA_ARQUIVO_ASSINADO);

        private readonly List<string> _erros = new List<string>();

        public AssinarController()
        {
            if (!_diretorioArquivoAssinado.Exists)
                _diretorioArquivoAssinado.Create();

            if (!_diretorioArquivoRecebido.Exists)
                _diretorioArquivoRecebido.Create();
        }

        [HttpGet]
        public string Inicializar()
            => "Iniciou com sucesso.";

        [HttpPost]
        [RequestSizeLimit(6000000000)]
        public IActionResult AssinarArquivo()
        {
            var arquivosRecebidos = HttpContext.Request.Form.Files;

            var jsonDados = HttpContext.Request.Form[AssinarHelper.FORM_KEY_JSON].ToString();
            _validarJson(jsonDados);

            var dadosAssinatura = JsonConvert.DeserializeObject<AssinarDTO>(jsonDados);
            _validarDadosAssinatura(dadosAssinatura);
            if (!_operacaoValida()) return BadRequest(_erros);

            var pdfRecebidoFileInfo = _salvarArquivoRecebido(AssinarHelper.FORM_KEY_PDF, arquivosRecebidos);
            var certificadoFileInfo = _salvarArquivoRecebido(AssinarHelper.FORM_KEY_CERTIFICADO_PFX, arquivosRecebidos);

            if (!_operacaoValida()) return BadRequest(_erros);

            var caminhoArquivoAssinado = _assinar(certificadoFileInfo, dadosAssinatura, pdfRecebidoFileInfo);
            if (!_operacaoValida()) return BadRequest(_erros);

            _excluirArquivosDescartaveis(pdfRecebidoFileInfo, certificadoFileInfo);

            return Ok(caminhoArquivoAssinado);
        }

        private void _validarDadosAssinatura(AssinarDTO dadosAssinatura)
        {
            _validarCampo(dadosAssinatura.Senha, "json.senha");
            _validarCampo(dadosAssinatura.Local, "json.local");
            _validarCampo(dadosAssinatura.Razao, "json.razao");
        }

        private void _validarJson(string jsonDados)
        {
            try
            {
                JsonConvert.DeserializeObject<AssinarDTO>(jsonDados);
            }
            catch (Exception erro)
            {
                _erros.Add("Não foi possivel ler os dados em json. Erro: " + erro.Message);
            }
        }

        private static void _excluirArquivosDescartaveis(FileInfo pdfRecebidoFileInfo, FileInfo certificadoFileInfo)
        {
            certificadoFileInfo.Delete();
            pdfRecebidoFileInfo.Delete();
        }

        private string _assinar(FileInfo certificadoFileInfo, AssinarDTO dadosAssinatura, FileInfo pdfFileInfo)
        {
            // codigo externo retirado do link: https://viewbag.wordpress.com/2019/12/24/pdf-digital-signatures-itext7-bouncy-castle-net-core/

            var diretorioCertificado = certificadoFileInfo.FullName;
            var senhaCertificado = dadosAssinatura.Senha.ToCharArray();

            var SRC = pdfFileInfo.FullName;
            var DEST = _diretorioArquivoAssinado.FullName + pdfFileInfo.Name;
            var urlArquivoAssinado = AssinarHelper.BASE_URL(Request) + PASTA_ARQUIVO_ASSINADO + pdfFileInfo.Name;

            try
            {
                using (var chaveStram = new FileStream(diretorioCertificado, FileMode.Open, FileAccess.Read))
                {
                    Pkcs12Store pk12;
                    try
                    { pk12 = new Pkcs12Store(chaveStram, senhaCertificado);}
                    catch 
                    {
                        _erros.Add("Não foi possivel abrir o certificado PFX, possiveis motivos: senha inválida para acessar o certificado, ou certificado corrompido.");
                        return "";
                    }
                    var alias = _obterAlias(pk12);
                    var pk = pk12.GetKey(alias).Key;

                    var chain = _gerarCertificadoChain(pk12, alias);
                    _gerarPdfAssinado(dadosAssinatura, SRC, DEST, pk, chain);
                }
            }
            catch (Exception erro)
            {
                _erros.Add("Não foi possivel assinar o arquivo. Ocorreram erros ao gerar o arquivo assiando. erro: " + erro.Message);
                return "";
            }
            return urlArquivoAssinado;
        }

        private static X509Certificate[] _gerarCertificadoChain(Pkcs12Store pk12, string alias)
        {
            X509CertificateEntry[] ce = pk12.GetCertificateChain(alias);
            X509Certificate[] chain = new X509Certificate[ce.Length];
            for (int k = 0; k < ce.Length; ++k)
            {
                chain[k] = ce[k].Certificate;
            }

            return chain;
        }

        private static string _obterAlias(Pkcs12Store pk12)
        {
            string alias = null;
            foreach (object a in pk12.Aliases)
            {
                alias = ((string)a);
                if (pk12.IsKeyEntry(alias))
                {
                    break;
                }
            }

            return alias;
        }

        private static void _gerarPdfAssinado(AssinarDTO dadosAssinatura, string SRC, string DEST, ICipherParameters pk, X509Certificate[] chain)
        {
            using (FileStream fileStramDestino = new FileStream(DEST, FileMode.Create))
            using (PdfReader reader = new PdfReader(SRC))
            {
                PdfSigner signer = new PdfSigner(reader, fileStramDestino, new StampingProperties());
                _addCarimbo(dadosAssinatura, chain, signer);

                //finalizando
                IExternalSignature pks = new PrivateKeySignature(pk, DigestAlgorithms.SHA256);

                signer.SignDetached(pks, chain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);
            }
        }

        private static void _addCarimbo(AssinarDTO dadosAssinatura, X509Certificate[] chain, PdfSigner signer)
        {
            PdfSignatureAppearance appearance = signer.GetSignatureAppearance();
            var informacoesCertificado = chain.First().SubjectDN.GetValueList();
            var assinante = informacoesCertificado[informacoesCertificado.Count -1];
            var textoCarimbo = $"Assinado digitalmente por: {assinante}. \n" +
                               $"Em: {dadosAssinatura.Local}. \n" +
                               $"Na data de: {DateTime.Now:dd/MM/yyyy} as {DateTime.Now:HH:mm}. \n" +
                               $"Razão: {dadosAssinatura.Razao}.";

            appearance.SetLayer2Text(textoCarimbo)
                .SetPageRect(new Rectangle(dadosAssinatura.GetPosicao().X, dadosAssinatura.GetPosicao().Y, dadosAssinatura.GetTamanho().X, dadosAssinatura.GetTamanho().Y))
                .SetLocation(dadosAssinatura.Local)
                .SetReason(dadosAssinatura.Razao)
                .SetSignatureCreator("SGI Digital Signature")
                .SetPageNumber(1)
                ;
            signer.SetFieldName("Campo de assinatura");
        }

        private void _validarCampo(object valor, string campo)
        {
            if (valor == null || valor.ToString() == "") _erros.Add($"o campo {campo} não encontrado");
        }

        private FileInfo _salvarArquivoRecebido(string formKey, IFormFileCollection arquivosRecebidos)
        {
            var arquivo = arquivosRecebidos[formKey];
            _validarCampo(arquivo, formKey);

            if (!_operacaoValida()) return null;

            var nomeArquivo = _diretorioArquivoRecebido.FullName + _requestId.ToString() + "." + arquivo.FileName;

            using (var stream = new FileStream(nomeArquivo, FileMode.Create))
            {
                arquivo.CopyTo(stream);
            }

            return new FileInfo(nomeArquivo);
        }



        private bool _operacaoValida() => !_erros.Any();
    }
}
