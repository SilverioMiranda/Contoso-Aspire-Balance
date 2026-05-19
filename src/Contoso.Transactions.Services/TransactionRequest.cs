using System.ComponentModel.DataAnnotations;

namespace Contoso.Transactions.Services
{
    public class TransactionRequest
    {
        [Required(ErrorMessage = "O campo Amount é obrigatório.")]
        [NonZeroDecimal(ErrorMessage = "O valor deve ser positivo ou negativo, mas não pode ser 0.")]
        public required decimal Amount { get; set; }

        [StringLength(200, ErrorMessage = "A descrição deve ter no máximo 200 caracteres.")]
        public string? Description { get; set; }
    }
}
