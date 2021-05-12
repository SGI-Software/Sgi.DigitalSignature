# Sgi.DigitalSignature
Serviço de assinatura digital para PDF

o serviço é utilizado atrasvés de um form data respondendo pelo metodo POST.
o endereço para acesso é http://minhapp/api/assinar

nele deve ser passado as seguintes informações:
- certificadoPfx: o arquivo .pfx referente ao certificado digital A1.
- pdf: PDF a ser assinado
- json: string representando um json com as demais informações necessárias para assinatura. Exemplo:
        {
            "senha": 1234,
            "local": "Estância Velha",
            "razao": "Testar",
            "posicao": {
                "x": 300,
                "y": 10
            },
            "tamanho": {
                "x": 300,
                "y": 50
            }
        }
        
    em c# a classe pode ser representada da seguinte forma:
    
    public class AssinarDTO
    {
        public string Senha { get; set; }
        public string Local { get; set; }
        public string Razao { get; set; }
        public Dimensao Posicao { get; set; }
        public Dimensao Tamanho { get; set; }
    }
    public class Dimensao
    {
        public float X { get; set; }
        public float Y { get; set; }
    }
