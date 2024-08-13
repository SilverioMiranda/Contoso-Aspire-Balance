using System.ComponentModel.DataAnnotations.Schema;

namespace Contoso.Data.Entities
{
    [Table("balances")]
    public class Balance
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTimeOffset Date { get; set; }
        public decimal Value { get; set; }
    }
}
