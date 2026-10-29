using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Circulation;
using College_Library_Management_System.ViewModels.Common;
using College_Library_Management_System.ViewModels.Fines;
using College_Library_Management_System.ViewModels.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace College_Library_Management_System.Services
{
    public class PdfReportService : IPdfReportService
    {
        private static readonly Color PrimaryColor = Color.FromHex("#1E3A8A"); // Navy blue
        private static readonly Color SecondaryColor = Color.FromHex("#475569"); // Slate gray
        private static readonly Color LightBg = Color.FromHex("#F8FAFC");
        private static readonly Color BorderColor = Color.FromHex("#CBD5E1");
        private static readonly Color SuccessColor = Color.FromHex("#15803D");
        private static readonly Color DangerColor = Color.FromHex("#B91C1C");

        static PdfReportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseSystemFonts = true;
            QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
        }

        #region 1. Collections Report PDF

        public byte[] GenerateCollectionsPdf(CollectionReportFilterViewModel report, DateTime generatedAt)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                    // Header
                    page.Header().Element(c => ComposeReportHeader(c,
                        "Financial Collections & Revenue Ledger",
                        $"Official fine payment transactions and revenue settlement audit",
                        generatedAt,
                        $"Date Range: {(report.StartDate.HasValue ? report.StartDate.Value.ToString("dd MMM yyyy") : "Earliest")} to {(report.EndDate.HasValue ? report.EndDate.Value.ToString("dd MMM yyyy") : "Latest")} | Payment Method: {(report.PaymentMethod.HasValue ? report.PaymentMethod.Value.ToString() : "All Methods")}"));

                    // Content
                    page.Content().PaddingTop(10).Column(column =>
                    {
                        var cashTotal = report.Transactions.Where(t => t.PaymentMethod == PaymentMethod.Cash).Sum(t => t.Amount);
                        var digitalTotal = report.Transactions.Where(t => t.PaymentMethod != PaymentMethod.Cash).Sum(t => t.Amount);

                        // Summary Cards Row
                        column.Item().PaddingBottom(12).Row(row =>
                        {
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Total Collections", $"Rs. {report.TotalCollected:N2}", SuccessColor));
                            row.Spacing(8);
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Total Receipts", $"{report.TransactionsCount} transactions", PrimaryColor));
                            row.Spacing(8);
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Cash Collections", $"Rs. {cashTotal:N2}", SecondaryColor));
                            row.Spacing(8);
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Online / UPI", $"Rs. {digitalTotal:N2}", SecondaryColor));
                        });

                        // Transactions Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(28); // #
                                cols.RelativeColumn(1.3f); // Receipt #
                                cols.RelativeColumn(1.2f); // Date
                                cols.RelativeColumn(1.5f); // Student
                                cols.RelativeColumn(1.0f); // Enrollment
                                cols.RelativeColumn(0.6f); // Dept
                                cols.RelativeColumn(2.0f); // Book Title & Accession
                                cols.RelativeColumn(1.1f); // Cashier
                                cols.RelativeColumn(0.9f); // Method
                                cols.RelativeColumn(1.1f); // Amount
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("#");
                                header.Cell().Element(HeaderStyle).Text("Receipt #");
                                header.Cell().Element(HeaderStyle).Text("Date & Time");
                                header.Cell().Element(HeaderStyle).Text("Student Name");
                                header.Cell().Element(HeaderStyle).Text("Enrollment");
                                header.Cell().Element(HeaderStyle).Text("Dept");
                                header.Cell().Element(HeaderStyle).Text("Book / Accession");
                                header.Cell().Element(HeaderStyle).Text("Cashier");
                                header.Cell().Element(HeaderStyle).Text("Method");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Amount (Rs.)");
                            });

                            int index = 1;
                            foreach (var tx in report.Transactions)
                            {
                                var bg = index % 2 == 0 ? LightBg : Colors.White;

                                table.Cell().Element(c => BodyStyle(c, bg)).Text(index.ToString());
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(tx.ReceiptNumber).Bold().FontColor(PrimaryColor);
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(tx.PaymentDate.ToString("dd-MMM-yyyy hh:mm tt"));
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(tx.StudentName);
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(tx.EnrollmentNumber);
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(tx.DepartmentCode);
                                table.Cell().Element(c => BodyStyle(c, bg)).Text($"{tx.BookTitle} ({tx.AccessionNumber})");
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(tx.CashierName);
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(tx.PaymentMethod.ToString());
                                table.Cell().Element(c => BodyStyle(c, bg)).AlignRight().Text($"Rs. {tx.Amount:N2}").Bold();

                                index++;
                            }

                            if (!report.Transactions.Any())
                            {
                                table.Cell().ColumnSpan(10).Padding(20).AlignCenter().Text("No financial transactions recorded matching the specified criteria.").Italic().FontColor(Colors.Grey.Medium);
                            }
                            else
                            {
                                // Grand Total Row
                                table.Cell().ColumnSpan(9).Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("GRAND TOTAL:").Bold();
                                table.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"Rs. {report.TotalCollected:N2}").Bold().FontColor(SuccessColor);
                            }
                        });
                    });

                    // Footer
                    page.Footer().Element(ComposeReportFooter);
                });
            });

            return doc.GeneratePdf();
        }

        #endregion

        #region 2. Overdue Books Report PDF

        public byte[] GenerateOverduePdf(OverdueReportFilterViewModel report, DateTime generatedAt)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                    // Header
                    var deptName = report.DepartmentId.HasValue && report.DepartmentOptions != null
                        ? report.DepartmentOptions.FirstOrDefault(d => d.Value == report.DepartmentId.Value.ToString())?.Text ?? $"Dept #{report.DepartmentId}"
                        : "All Academic Departments";

                    page.Header().Element(c => ComposeReportHeader(c,
                        "Overdue Books & Borrowers Audit Report",
                        "Urgent circulation recovery listing with student contact details and late fees",
                        generatedAt,
                        $"Department: {deptName} | Min Overdue Days: {(report.MinDaysOverdue.HasValue ? $"{report.MinDaysOverdue.Value} days" : "Any")}"));

                    // Content
                    page.Content().PaddingTop(10).Column(column =>
                    {
                        // Summary Cards
                        column.Item().PaddingBottom(12).Row(row =>
                        {
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Total Overdue Loans", $"{report.TotalOverdueCount} books", DangerColor));
                            row.Spacing(10);
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Total Accrued Late Fines", $"Rs. {report.TotalEstimatedFine:N2}", DangerColor));
                            row.Spacing(10);
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Daily Late Fee Rate", "Rs. 5.00 / day", SecondaryColor));
                        });

                        // Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(28); // #
                                cols.RelativeColumn(1.4f); // Student
                                cols.RelativeColumn(1.0f); // Enrollment
                                cols.RelativeColumn(0.6f); // Dept
                                cols.RelativeColumn(1.6f); // Contact (Phone / Email)
                                cols.RelativeColumn(2.0f); // Book Title
                                cols.RelativeColumn(0.9f); // Accession
                                cols.RelativeColumn(0.9f); // Due Date
                                cols.RelativeColumn(0.8f); // Overdue
                                cols.RelativeColumn(1.0f); // Est. Fine
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("#");
                                header.Cell().Element(HeaderStyle).Text("Student Name");
                                header.Cell().Element(HeaderStyle).Text("Enrollment");
                                header.Cell().Element(HeaderStyle).Text("Dept");
                                header.Cell().Element(HeaderStyle).Text("Contact Details");
                                header.Cell().Element(HeaderStyle).Text("Book Title");
                                header.Cell().Element(HeaderStyle).Text("Accession #");
                                header.Cell().Element(HeaderStyle).Text("Due Date");
                                header.Cell().Element(HeaderStyle).AlignCenter().Text("Overdue");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Est. Fine (Rs.)");
                            });

                            int index = 1;
                            foreach (var item in report.Items)
                            {
                                var bg = index % 2 == 0 ? LightBg : Colors.White;

                                table.Cell().Element(c => BodyStyle(c, bg)).Text(index.ToString());
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(item.StudentName).Bold();
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(item.EnrollmentNumber);
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(item.DepartmentCode);
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(text =>
                                {
                                    if (!string.IsNullOrEmpty(item.Phone)) text.Line($"Ph: {item.Phone}");
                                    if (!string.IsNullOrEmpty(item.Email)) text.Line(item.Email);
                                });
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(item.BookTitle);
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(item.AccessionNumber).Bold().FontColor(PrimaryColor);
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(item.DueAt.ToString("dd-MMM-yyyy"));
                                table.Cell().Element(c => BodyStyle(c, bg)).AlignCenter().Text($"{item.OverdueDays} days").Bold().FontColor(DangerColor);
                                table.Cell().Element(c => BodyStyle(c, bg)).AlignRight().Text($"Rs. {item.EstimatedFine:N2}").Bold().FontColor(DangerColor);

                                index++;
                            }

                            if (!report.Items.Any())
                            {
                                table.Cell().ColumnSpan(10).Padding(20).AlignCenter().Text("Excellent! No overdue unreturned book loans found matching the criteria.").Italic().FontColor(SuccessColor);
                            }
                            else
                            {
                                // Total row
                                table.Cell().ColumnSpan(9).Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"TOTAL ESTIMATED OVERDUE FINES ({report.TotalOverdueCount} books):").Bold();
                                table.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"Rs. {report.TotalEstimatedFine:N2}").Bold().FontColor(DangerColor);
                            }
                        });
                    });

                    // Footer
                    page.Footer().Element(ComposeReportFooter);
                });
            });

            return doc.GeneratePdf();
        }

        #endregion

        #region 3. Inventory Status Report PDF

        public byte[] GenerateInventoryPdf(InventoryReportViewModel report, DateTime generatedAt)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                    // Header
                    page.Header().Element(c => ComposeReportHeader(c,
                        "Library Inventory & Asset Status Report",
                        "Comprehensive catalog audit, shelf availability, and collection distribution",
                        generatedAt,
                        $"Scope: Entire Campus Central Library Collection"));

                    // Content
                    page.Content().PaddingTop(10).Column(column =>
                    {
                        // Summary Cards Row 1
                        column.Item().PaddingBottom(6).Row(row =>
                        {
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Catalog Titles", report.TotalCatalogTitles.ToString(), PrimaryColor));
                            row.Spacing(8);
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Total Physical Copies", report.TotalPhysicalCopies.ToString(), PrimaryColor));
                            row.Spacing(8);
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Available on Stacks", report.AvailableCount.ToString(), SuccessColor));
                            row.Spacing(8);
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Currently Issued", report.IssuedCount.ToString(), Color.FromHex("#0284C7")));
                        });

                        // Summary Cards Row 2
                        column.Item().PaddingBottom(14).Row(row =>
                        {
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Damaged Copies", report.DamagedCount.ToString(), Color.FromHex("#D97706")));
                            row.Spacing(8);
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Reported Lost", report.LostCount.ToString(), DangerColor));
                            row.Spacing(8);
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Withdrawn / Retired", report.WithdrawnCount.ToString(), SecondaryColor));
                            row.Spacing(8);
                            var availRate = report.TotalPhysicalCopies > 0
                                ? (report.AvailableCount * 100.0 / report.TotalPhysicalCopies).ToString("F1") + "%"
                                : "N/A";
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Shelf Availability Rate", availRate, SuccessColor));
                        });

                        // Section 1: Category Breakdown Table
                        column.Item().PaddingBottom(4).Text("1. Collection Inventory Breakdown by Category").Bold().FontSize(10).FontColor(PrimaryColor);

                        column.Item().PaddingBottom(14).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(26);
                                cols.RelativeColumn(2.4f); // Category
                                cols.RelativeColumn(0.9f); // Titles
                                cols.RelativeColumn(0.9f); // Total Copies
                                cols.RelativeColumn(0.9f); // Available
                                cols.RelativeColumn(0.9f); // Issued
                                cols.RelativeColumn(0.9f); // Utilization %
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("#");
                                header.Cell().Element(HeaderStyle).Text("Category Name");
                                header.Cell().Element(HeaderStyle).AlignCenter().Text("Titles");
                                header.Cell().Element(HeaderStyle).AlignCenter().Text("Total Copies");
                                header.Cell().Element(HeaderStyle).AlignCenter().Text("Available");
                                header.Cell().Element(HeaderStyle).AlignCenter().Text("Issued");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Circulation %");
                            });

                            int idx = 1;
                            foreach (var cat in report.CategoryBreakdown)
                            {
                                var bg = idx % 2 == 0 ? LightBg : Colors.White;
                                var rate = cat.CopyCount > 0 ? (cat.IssuedCopies * 100.0 / cat.CopyCount).ToString("F1") + "%" : "0.0%";

                                table.Cell().Element(c => BodyStyle(c, bg)).Text(idx.ToString());
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(cat.CategoryName).Bold();
                                table.Cell().Element(c => BodyStyle(c, bg)).AlignCenter().Text(cat.BookCount.ToString());
                                table.Cell().Element(c => BodyStyle(c, bg)).AlignCenter().Text(cat.CopyCount.ToString());
                                table.Cell().Element(c => BodyStyle(c, bg)).AlignCenter().Text(cat.AvailableCopies.ToString()).FontColor(SuccessColor).Bold();
                                table.Cell().Element(c => BodyStyle(c, bg)).AlignCenter().Text(cat.IssuedCopies.ToString()).FontColor(PrimaryColor);
                                table.Cell().Element(c => BodyStyle(c, bg)).AlignRight().Text(rate);

                                idx++;
                            }
                        });

                        // Section 2: Department Borrowing & Student Distribution
                        column.Item().PaddingBottom(4).Text("2. Departmental Student Registry & Active Borrowing").Bold().FontSize(10).FontColor(PrimaryColor);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(26);
                                cols.RelativeColumn(2.6f); // Department
                                cols.RelativeColumn(0.8f); // Code
                                cols.RelativeColumn(1.0f); // Enrolled Students
                                cols.RelativeColumn(1.0f); // Active Loans
                                cols.RelativeColumn(1.0f); // Avg Books/Student
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("#");
                                header.Cell().Element(HeaderStyle).Text("Department Name");
                                header.Cell().Element(HeaderStyle).AlignCenter().Text("Code");
                                header.Cell().Element(HeaderStyle).AlignCenter().Text("Enrolled Students");
                                header.Cell().Element(HeaderStyle).AlignCenter().Text("Active Loans");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Active Rate");
                            });

                            int idx = 1;
                            foreach (var dept in report.DepartmentBreakdown)
                            {
                                var bg = idx % 2 == 0 ? LightBg : Colors.White;
                                var avg = dept.StudentCount > 0 ? (dept.ActiveLoansCount * 1.0 / dept.StudentCount).ToString("F2") : "0.00";

                                table.Cell().Element(c => BodyStyle(c, bg)).Text(idx.ToString());
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(dept.DepartmentName).Bold();
                                table.Cell().Element(c => BodyStyle(c, bg)).AlignCenter().Text(dept.DepartmentCode);
                                table.Cell().Element(c => BodyStyle(c, bg)).AlignCenter().Text(dept.StudentCount.ToString());
                                table.Cell().Element(c => BodyStyle(c, bg)).AlignCenter().Text(dept.ActiveLoansCount.ToString()).Bold().FontColor(PrimaryColor);
                                table.Cell().Element(c => BodyStyle(c, bg)).AlignRight().Text($"{avg} bks/std");

                                idx++;
                            }
                        });
                    });

                    // Footer
                    page.Footer().Element(ComposeReportFooter);
                });
            });

            return doc.GeneratePdf();
        }

        #endregion

        #region 4. Fine Payment Receipt Voucher PDF

        public byte[] GenerateFineReceiptPdf(FineReceiptViewModel receipt, DateTime generatedAt)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Lato"));

                    page.Content().Border(1.5f).BorderColor(PrimaryColor).Padding(24).Column(col =>
                    {
                        // Institution Header
                        col.Item().AlignCenter().Text("COLLEGE CENTRAL LIBRARY").Bold().FontSize(17).FontColor(PrimaryColor);
                        col.Item().AlignCenter().Text("Circulation & Fine Management Section").FontSize(10).FontColor(SecondaryColor);
                        col.Item().AlignCenter().Text("Campus Administration Complex, Central Library Building").FontSize(8.5f).FontColor(Colors.Grey.Medium);

                        col.Item().PaddingVertical(10).AlignCenter().Container().Background(Color.FromHex("#DCFCE7")).Border(1).BorderColor(Color.FromHex("#86EFAC")).PaddingVertical(4).PaddingHorizontal(20).Text("OFFICIAL FINE PAYMENT RECEIPT").Bold().FontSize(11).FontColor(SuccessColor);

                        // Receipt Number & Date Banner
                        col.Item().PaddingBottom(12).BorderBottom(1).BorderColor(BorderColor).PaddingBottom(8).Row(row =>
                        {
                            row.RelativeItem().Text(t =>
                            {
                                t.Span("Receipt Number: ").FontColor(SecondaryColor);
                                t.Span(receipt.ReceiptNumber).Bold().FontSize(11).FontColor(PrimaryColor);
                            });
                            row.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span("Payment Date: ").FontColor(SecondaryColor);
                                t.Span(receipt.PaymentDate.ToString("dd MMM yyyy, hh:mm tt") + " (IST)").Bold();
                            });
                        });

                        // Two column box: Student Info & Circulation Info
                        col.Item().PaddingBottom(14).Row(row =>
                        {
                            // Student Info
                            row.RelativeItem().Border(1).BorderColor(BorderColor).Background(LightBg).Padding(10).Column(c =>
                            {
                                c.Item().Text("STUDENT BORROWER").Bold().FontSize(9).FontColor(SecondaryColor);
                                c.Item().PaddingTop(4).Text(receipt.StudentName).Bold().FontSize(11);
                                c.Item().Text($"Enrollment No: {receipt.StudentEnrollment}").FontSize(9);
                                c.Item().Text($"Department: {receipt.DepartmentName}").FontSize(9);
                            });

                            row.Spacing(12);

                            // Book & Loan Info
                            row.RelativeItem().Border(1).BorderColor(BorderColor).Background(LightBg).Padding(10).Column(c =>
                            {
                                c.Item().Text("CIRCULATION DETAILS").Bold().FontSize(9).FontColor(SecondaryColor);
                                c.Item().PaddingTop(4).Text(receipt.BookTitle).Bold().FontSize(10);
                                c.Item().Text($"Accession No: {receipt.AccessionNumber}").Bold().FontColor(PrimaryColor);
                                c.Item().Text($"Overdue Duration: {receipt.OverdueDays} days overdue");
                                c.Item().Text($"Standard Rate Applied: Rs. {receipt.DailyFineRate:F2} / day");
                            });
                        });

                        // Payment Breakdown Table
                        col.Item().PaddingBottom(14).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(30);
                                cols.RelativeColumn(3.5f);
                                cols.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("#");
                                header.Cell().Element(HeaderStyle).Text("Item Description");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Amount (Rs.)");
                            });

                            // Row 1: Overdue Late Fee Assessed
                            table.Cell().Element(c => BodyStyle(c, Colors.White)).Text("1");
                            table.Cell().Element(c => BodyStyle(c, Colors.White)).Text(t =>
                            {
                                t.Line("Library Overdue Fine Settlement").Bold();
                                t.Span($"Assessment: {receipt.OverdueDays} days overdue @ Rs. {receipt.DailyFineRate:F2}/day").FontSize(7.5f).FontColor(SecondaryColor);
                                if (!string.IsNullOrWhiteSpace(receipt.Remarks))
                                {
                                    t.Line($"Remarks: {receipt.Remarks}").FontSize(7.5f).Italic().FontColor(Colors.Grey.Darken1);
                                }
                            });
                            table.Cell().Element(c => BodyStyle(c, Colors.White)).AlignRight().Text($"Rs. {receipt.AmountPaid:N2}");

                            // Total Paid Row
                            table.Cell().ColumnSpan(2).Background(Color.FromHex("#DCFCE7")).Padding(8).AlignRight().Text("TOTAL AMOUNT PAID (FULL SETTLEMENT):").Bold().FontColor(SuccessColor);
                            table.Cell().Background(Color.FromHex("#DCFCE7")).Padding(8).AlignRight().Text($"Rs. {receipt.AmountPaid:N2}").Bold().FontSize(11).FontColor(SuccessColor);

                            // Balance
                            table.Cell().ColumnSpan(2).Background(LightBg).Padding(6).AlignRight().Text("Remaining Balance Pending:").Bold();
                            table.Cell().Background(LightBg).Padding(6).AlignRight().Text("Rs. 0.00 (Nil)").Bold().FontColor(SuccessColor);
                        });

                        // Payment Method & Cashier Metadata Box
                        col.Item().PaddingBottom(20).Border(1).BorderColor(BorderColor).Padding(8).Row(r =>
                        {
                            r.RelativeItem().Text(t =>
                            {
                                t.Span("Payment Method: ").FontColor(SecondaryColor);
                                t.Span(receipt.PaymentMethod.ToString()).Bold();
                            });
                            r.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span("Received By (Staff): ").FontColor(SecondaryColor);
                                t.Span(receipt.ReceivedByStaffName).Bold();
                            });
                        });

                        // Signature & Stamp Section
                        col.Item().PaddingTop(25).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().LineHorizontal(0.8f).LineColor(Colors.Grey.Medium);
                                c.Item().PaddingTop(4).AlignCenter().Text("Student / Payer Signature").FontSize(8.5f).FontColor(SecondaryColor);
                            });

                            r.Spacing(40);

                            r.RelativeItem().Column(c =>
                            {
                                c.Item().LineHorizontal(0.8f).LineColor(Colors.Grey.Medium);
                                c.Item().PaddingTop(4).AlignCenter().Text("Authorized Librarian Signature & Seal").FontSize(8.5f).FontColor(SecondaryColor);
                            });
                        });

                        // Security & Verification Notice
                        col.Item().PaddingTop(25).AlignCenter().Text(t =>
                        {
                            t.Span("Security Verification: ").Bold().FontSize(7.5f).FontColor(SecondaryColor);
                            t.Span($"Official computer-generated receipt voucher | Transaction verified on {receipt.PaymentDate:dd-MMM-yyyy hh:mm:ss} | Non-transferable").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }

        #endregion

        #region 5. Circulation History Report PDF

        public byte[] GenerateCirculationHistoryPdf(PaginatedList<LoanHistoryItemViewModel> items, LoanHistoryFilterViewModel filter, DateTime generatedAt)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                    var tabTitle = (filter.ActiveTab ?? "all").ToLowerInvariant() switch
                    {
                        "issued" => "Issued Books Circulation History",
                        "returned" => "Returned Books Circulation History",
                        _ => "All Circulation & Loan History Records"
                    };

                    // Header
                    page.Header().Element(c => ComposeReportHeader(c,
                        tabTitle,
                        "Complete chronological audit ledger of library book issues, returns, and borrower activity",
                        generatedAt,
                        $"Filter: Tab={filter.ActiveTab?.ToUpper() ?? "ALL"} | Search: '{(string.IsNullOrWhiteSpace(filter.SearchTerm) ? "None" : filter.SearchTerm)}' | Status: {(filter.Status.HasValue ? filter.Status.Value.ToString() : "All Statuses")}"));

                    // Content
                    page.Content().PaddingTop(10).Column(column =>
                    {
                        // Summary Cards Row
                        column.Item().PaddingBottom(12).Row(row =>
                        {
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Total Loans Logged", filter.Summary.TotalLoans.ToString(), PrimaryColor));
                            row.Spacing(8);
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Currently Issued", $"{filter.Summary.ActiveLoansCount} books", Color.FromHex("#0284C7")));
                            row.Spacing(8);
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Successfully Returned", $"{filter.Summary.ReturnedLoansCount} books", SuccessColor));
                            row.Spacing(8);
                            row.RelativeItem().Element(c => ComposeMetricCard(c, "Overdue / Fined", $"{filter.Summary.OverdueLoansCount} overdue", DangerColor));
                        });

                        // Circulation Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(26); // #
                                cols.RelativeColumn(1.0f); // Accession
                                cols.RelativeColumn(2.0f); // Book Title
                                cols.RelativeColumn(1.4f); // Student
                                cols.RelativeColumn(0.6f); // Dept
                                cols.RelativeColumn(1.1f); // Issued On
                                cols.RelativeColumn(1.0f); // Issued By
                                cols.RelativeColumn(0.9f); // Due Date
                                cols.RelativeColumn(1.1f); // Returned On
                                cols.RelativeColumn(0.9f); // Status
                                cols.RelativeColumn(0.9f); // Fine (Rs.)
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("#");
                                header.Cell().Element(HeaderStyle).Text("Accession #");
                                header.Cell().Element(HeaderStyle).Text("Book Title");
                                header.Cell().Element(HeaderStyle).Text("Student Borrower");
                                header.Cell().Element(HeaderStyle).Text("Dept");
                                header.Cell().Element(HeaderStyle).Text("Issued On");
                                header.Cell().Element(HeaderStyle).Text("Issued By");
                                header.Cell().Element(HeaderStyle).Text("Due Date");
                                header.Cell().Element(HeaderStyle).Text("Returned On");
                                header.Cell().Element(HeaderStyle).Text("Status");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Fine (Rs.)");
                            });

                            int index = 1;
                            foreach (var loan in items)
                            {
                                var bg = index % 2 == 0 ? LightBg : Colors.White;

                                table.Cell().Element(c => BodyStyle(c, bg)).Text(index.ToString());
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(loan.AccessionNumber).Bold().FontColor(PrimaryColor);
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(loan.BookTitle);
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(text =>
                                {
                                    text.Line(loan.StudentName).Bold();
                                    text.Span(loan.StudentEnrollment).FontSize(7.5f).FontColor(SecondaryColor);
                                });
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(loan.DepartmentCode);
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(loan.IssuedAt.ToString("dd-MMM-yyyy"));
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(loan.IssuedBy);
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(loan.DueAt.ToString("dd-MMM-yyyy"));
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(loan.ReturnedAt.HasValue ? loan.ReturnedAt.Value.ToString("dd-MMM-yyyy") : "Active (Out)");

                                // Status
                                table.Cell().Element(c => BodyStyle(c, bg)).Text(loan.Status == LoanStatus.Active && loan.IsOverdue ? "Overdue" : loan.Status.ToString()).Bold()
                                    .FontColor(loan.Status == LoanStatus.Returned ? SuccessColor : loan.IsOverdue ? DangerColor : PrimaryColor);

                                // Fine
                                table.Cell().Element(c => BodyStyle(c, bg)).AlignRight().Text(loan.EstimatedOrActualFine > 0 ? $"Rs. {loan.EstimatedOrActualFine:N2}" : "-")
                                    .FontColor(loan.EstimatedOrActualFine > 0 ? DangerColor : Colors.Grey.Darken1);

                                index++;
                            }

                            if (!items.Any())
                            {
                                table.Cell().ColumnSpan(11).Padding(20).AlignCenter().Text("No circulation history records found matching criteria.").Italic().FontColor(Colors.Grey.Medium);
                            }
                            else
                            {
                                table.Cell().ColumnSpan(11).Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"Total Records in Report: {items.Count} of {items.TotalCount} total").Bold();
                            }
                        });
                    });

                    // Footer
                    page.Footer().Element(ComposeReportFooter);
                });
            });

            return doc.GeneratePdf();
        }

        #endregion

        #region Helper Styles and Components

        private static void ComposeReportHeader(IContainer container, string title, string subtitle, DateTime generatedAt, string filterDetails)
        {
            container.BorderBottom(1.5f).BorderColor(PrimaryColor).PaddingBottom(8).Row(row =>
            {
                row.RelativeItem(2.5f).Column(col =>
                {
                    col.Item().Text("COLLEGE CENTRAL LIBRARY").Bold().FontSize(13).FontColor(PrimaryColor);
                    col.Item().Text(title).Bold().FontSize(15).FontColor(Color.FromHex("#0F172A"));
                    col.Item().Text(subtitle).FontSize(8.5f).FontColor(SecondaryColor);
                });

                row.RelativeItem(1.5f).AlignRight().Column(col =>
                {
                    col.Item().Text($"Generated: {generatedAt:dd MMM yyyy, hh:mm tt} (IST)").FontSize(8).FontColor(SecondaryColor);
                    col.Item().Text("Official Library Records System").FontSize(8).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(3).Text(filterDetails).FontSize(7.5f).FontColor(PrimaryColor).Italic();
                });
            });
        }

        private static void ComposeReportFooter(IContainer container)
        {
            container.BorderTop(0.5f).BorderColor(BorderColor).PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("College Central Library Management System").FontSize(7.5f).Bold().FontColor(SecondaryColor);
                    text.Span(" | Official Document | Confidential").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                });

                row.AutoItem().Text(text =>
                {
                    text.Span("Page ").FontSize(8).FontColor(SecondaryColor);
                    text.CurrentPageNumber().FontSize(8).Bold().FontColor(PrimaryColor);
                    text.Span(" of ").FontSize(8).FontColor(SecondaryColor);
                    text.TotalPages().FontSize(8).Bold().FontColor(PrimaryColor);
                });
            });
        }

        private static void ComposeMetricCard(IContainer container, string label, string value, Color accentColor)
        {
            container.Border(1).BorderColor(BorderColor).Background(LightBg).Padding(8).Column(c =>
            {
                c.Item().Text(label.ToUpper()).FontSize(7.5f).Bold().FontColor(SecondaryColor);
                c.Item().PaddingTop(2).Text(value).FontSize(13).Bold().FontColor(accentColor);
            });
        }

        private static IContainer HeaderStyle(IContainer container)
        {
            return container
                .Background(PrimaryColor)
                .PaddingVertical(5)
                .PaddingHorizontal(4)
                .DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(8f));
        }

        private static IContainer BodyStyle(IContainer container, Color bg)
        {
            return container
                .Background(bg)
                .BorderBottom(0.5f)
                .BorderColor(BorderColor)
                .PaddingVertical(4)
                .PaddingHorizontal(4);
        }

        #endregion
    }
}
