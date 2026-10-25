using College_Library_Management_System.Models.Enums;

namespace College_Library_Management_System.Models
{
    public class FinePayment
    {
        public int FinePaymentId { get; set; }
        public int FineId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public DateTime ReceivedAt { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public string ReceivedByUserId { get; set; } = string.Empty;
        public string? Remarks { get; set; }

        public virtual Fine Fine { get; set; } = null!;
        public virtual ApplicationUser ReceivedByUser { get; set; } = null!;
    }
}
