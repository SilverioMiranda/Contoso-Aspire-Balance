using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Contoso.Data.Entities
{
    [Table("transactions")]
    public class Transaction
    {
        /// <summary>
        /// Poderia ser Guid, mas ai teriamos que nos preocupar com a fragmentação do indice no banco de dados, talvez usar um valor gerado manualmente usando o UUIDV7
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [JsonIgnore]
        public Guid RequestId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public string? Description { get; set; }
        public decimal Value { get; set; }
    }
}
