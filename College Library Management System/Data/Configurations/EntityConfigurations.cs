using College_Library_Management_System.Models;
using College_Library_Management_System.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace College_Library_Management_System.Data.Configurations
{
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.ToTable("Departments");
            builder.HasKey(d => d.DepartmentId);

            builder.Property(d => d.DepartmentName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(d => d.DepartmentCode)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasIndex(d => d.DepartmentCode)
                .IsUnique();

            builder.HasMany(d => d.Students)
                .WithOne(s => s.Department)
                .HasForeignKey(s => s.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.ToTable("Students");
            builder.HasKey(s => s.StudentId);

            builder.Property(s => s.EnrollmentNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(s => s.EnrollmentNumber)
                .IsUnique();

            builder.Property(s => s.FullName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(s => s.Email)
                .HasMaxLength(150);

            builder.Property(s => s.Phone)
                .HasMaxLength(20);

            builder.Property(s => s.Semester)
                .IsRequired();

            builder.Property(s => s.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.UserId)
                .HasMaxLength(450);

            builder.HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(s => s.Loans)
                .WithOne(l => l.Student)
                .HasForeignKey(l => l.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");
            builder.HasKey(c => c.CategoryId);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(c => c.Name)
                .IsUnique();

            builder.Property(c => c.Description)
                .HasMaxLength(500);

            builder.HasMany(c => c.Books)
                .WithOne(b => b.Category)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class PublisherConfiguration : IEntityTypeConfiguration<Publisher>
    {
        public void Configure(EntityTypeBuilder<Publisher> builder)
        {
            builder.ToTable("Publishers");
            builder.HasKey(p => p.PublisherId);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.HasIndex(p => p.Name)
                .IsUnique();

            builder.Property(p => p.Address)
                .HasMaxLength(250);

            builder.Property(p => p.ContactEmail)
                .HasMaxLength(150);

            builder.HasMany(p => p.Books)
                .WithOne(b => b.Publisher)
                .HasForeignKey(b => b.PublisherId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class AuthorConfiguration : IEntityTypeConfiguration<Author>
    {
        public void Configure(EntityTypeBuilder<Author> builder)
        {
            builder.ToTable("Authors");
            builder.HasKey(a => a.AuthorId);

            builder.Property(a => a.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(a => a.Biography)
                .HasMaxLength(1000);

            builder.HasMany(a => a.BookAuthors)
                .WithOne(ba => ba.Author)
                .HasForeignKey(ba => ba.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class BookConfiguration : IEntityTypeConfiguration<Book>
    {
        public void Configure(EntityTypeBuilder<Book> builder)
        {
            builder.ToTable("Books");
            builder.HasKey(b => b.BookId);

            builder.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(b => b.ISBN)
                .HasMaxLength(20);

            builder.HasIndex(b => b.ISBN)
                .IsUnique()
                .HasFilter("[ISBN] IS NOT NULL AND [ISBN] <> ''");

            builder.Property(b => b.Description)
                .HasMaxLength(1000);

            builder.HasMany(b => b.Copies)
                .WithOne(c => c.Book)
                .HasForeignKey(c => c.BookId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class BookAuthorConfiguration : IEntityTypeConfiguration<BookAuthor>
    {
        public void Configure(EntityTypeBuilder<BookAuthor> builder)
        {
            builder.ToTable("BookAuthors");
            builder.HasKey(ba => new { ba.BookId, ba.AuthorId });

            builder.HasOne(ba => ba.Book)
                .WithMany(b => b.BookAuthors)
                .HasForeignKey(ba => ba.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ba => ba.Author)
                .WithMany(a => a.BookAuthors)
                .HasForeignKey(ba => ba.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
    {
        public void Configure(EntityTypeBuilder<BookCopy> builder)
        {
            builder.ToTable("BookCopies");
            builder.HasKey(bc => bc.BookCopyId);

            builder.Property(bc => bc.AccessionNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(bc => bc.AccessionNumber)
                .IsUnique();

            builder.Property(bc => bc.ShelfLocation)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(bc => bc.Status)
                .IsRequired();

            builder.Property(bc => bc.ConditionNotes)
                .HasMaxLength(500);

            builder.Property(bc => bc.RowVersion)
                .IsRowVersion();

            builder.HasMany(bc => bc.Loans)
                .WithOne(l => l.BookCopy)
                .HasForeignKey(l => l.BookCopyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class LoanConfiguration : IEntityTypeConfiguration<Loan>
    {
        public void Configure(EntityTypeBuilder<Loan> builder)
        {
            builder.ToTable("Loans");
            builder.HasKey(l => l.LoanId);

            builder.Property(l => l.Status)
                .IsRequired();

            builder.Property(l => l.RenewalCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(l => l.IssuedByUserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(l => l.ReceivedByUserId)
                .HasMaxLength(450);

            builder.Property(l => l.RowVersion)
                .IsRowVersion();

            // Filtered unique index: at most one active loan per physical BookCopy
            builder.HasIndex(l => l.BookCopyId)
                .IsUnique()
                .HasFilter($"[Status] = {(int)LoanStatus.Active}");

            builder.HasIndex(l => new { l.StudentId, l.Status });
            builder.HasIndex(l => l.DueAt);

            builder.HasOne(l => l.IssuedByUser)
                .WithMany(u => u.IssuedLoans)
                .HasForeignKey(l => l.IssuedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(l => l.ReceivedByUser)
                .WithMany(u => u.ReceivedLoans)
                .HasForeignKey(l => l.ReceivedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class FineConfiguration : IEntityTypeConfiguration<Fine>
    {
        public void Configure(EntityTypeBuilder<Fine> builder)
        {
            builder.ToTable("Fines");
            builder.HasKey(f => f.FineId);

            builder.HasIndex(f => f.LoanId)
                .IsUnique();

            builder.Property(f => f.FineAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(f => f.DailyFineRateApplied)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(f => f.AmountPaid)
                .HasPrecision(18, 2)
                .IsRequired()
                .HasDefaultValue(0m);

            builder.Property(f => f.WaivedAmount)
                .HasPrecision(18, 2)
                .IsRequired()
                .HasDefaultValue(0m);

            builder.Property(f => f.Status)
                .IsRequired();

            builder.Property(f => f.WaiverReason)
                .HasMaxLength(500);

            builder.HasOne(f => f.Loan)
                .WithOne(l => l.Fine)
                .HasForeignKey<Fine>(f => f.LoanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.WaivedByUser)
                .WithMany(u => u.WaivedFines)
                .HasForeignKey(f => f.WaivedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class FinePaymentConfiguration : IEntityTypeConfiguration<FinePayment>
    {
        public void Configure(EntityTypeBuilder<FinePayment> builder)
        {
            builder.ToTable("FinePayments");
            builder.HasKey(fp => fp.FinePaymentId);

            builder.HasIndex(fp => fp.FineId)
                .IsUnique();

            builder.Property(fp => fp.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(fp => fp.PaymentMethod)
                .IsRequired();

            builder.Property(fp => fp.ReceiptNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(fp => fp.ReceiptNumber)
                .IsUnique();

            builder.Property(fp => fp.ReceivedByUserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(fp => fp.Remarks)
                .HasMaxLength(500);

            builder.HasOne(fp => fp.Fine)
                .WithOne(f => f.FinePayment)
                .HasForeignKey<FinePayment>(fp => fp.FineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(fp => fp.ReceivedByUser)
                .WithMany(u => u.ReceivedPayments)
                .HasForeignKey(fp => fp.ReceivedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class LibrarySettingConfiguration : IEntityTypeConfiguration<LibrarySetting>
    {
        public void Configure(EntityTypeBuilder<LibrarySetting> builder)
        {
            builder.ToTable("LibrarySettings");
            builder.HasKey(ls => ls.LibrarySettingId);

            builder.Property(ls => ls.DailyFineRate)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(ls => ls.LoanPeriodDays)
                .IsRequired()
                .HasDefaultValue(14);

            builder.Property(ls => ls.MaxBooksPerStudent)
                .IsRequired()
                .HasDefaultValue(3);

            builder.Property(ls => ls.BlockIssueWhenOverdue)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(ls => ls.AllowRenewal)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(ls => ls.MaxRenewalsPerLoan)
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(ls => ls.UpdatedByUserId)
                .HasMaxLength(450);
        }
    }

    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(al => al.AuditLogId);

            builder.Property(al => al.UserId)
                .HasMaxLength(450);

            builder.Property(al => al.UserEmail)
                .HasMaxLength(150);

            builder.Property(al => al.Action)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(al => al.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(al => al.EntityId)
                .HasMaxLength(50);

            builder.Property(al => al.Details)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(al => al.IpAddress)
                .HasMaxLength(50);

            builder.HasIndex(al => al.Timestamp);
            builder.HasIndex(al => al.Action);
            builder.HasIndex(al => al.EntityType);
        }
    }
}
