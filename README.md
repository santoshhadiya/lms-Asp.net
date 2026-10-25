# College Library Management System

A production-grade, secure, and responsive **College Library Management System** engineered with **ASP.NET Core MVC (.NET 10)**, **C#**, **Entity Framework Core 10**, and **Microsoft SQL Server**.

---

## 1. Key Business Rules & Invariants

| Policy Rule | Implementation & Guarantee |
| :--- | :--- |
| **No Membership Fee** | Students register directly using unique institutional Enrollment Numbers with academic department and semester tracking. |
| **Borrowing Limit** | Strict maximum of **3 active books** per student (`LibrarySetting.MaxBooksPerStudent = 3`). Enforced at both service and database transaction boundaries. |
| **Loan Period** | Default loan duration of **14 calendar days** (`LibrarySetting.LoanPeriodDays = 14`). Due date computed using IST date arithmetic. |
| **Overdue Fine Rate** | **₹5.00 per overdue calendar day** (`max(0, ReturnDate.Date - DueDate.Date) * DailyFineRate`). Returning on or before the due date incurs **₹0.00** fine. |
| **Overdue Borrowing Block** | A student with ANY unreturned overdue active loan is **strictly blocked** from borrowing new books until the overdue item is returned. |
| **Single Active Loan Per Copy** | Filtered unique index `IX_Loans_BookCopyId` (`WHERE [Status] = 0`) guarantees at the SQL Server engine level that a physical copy cannot be issued twice concurrently. |
| **Renewals** | Maximum **1 renewal per loan** (`MaxRenewalsPerLoan = 1`). Eligible **only if** the loan is currently active and **not already overdue**. Extends due date by an additional 14 days. |
| **Offline Full Payments** | Fine payments are collected strictly offline at the library counter. **Partial payments are strictly prohibited**; the full outstanding balance must be cleared in a single transaction. |
| **Payment Receipts** | Every fine payment automatically generates a unique receipt voucher (`REC-yyyyMMdd-XXXXXX`) with a dedicated printable view (`@media print`). |
| **Administrative Waivers** | Only users in the `Admin` role can waive overdue fines. A mandatory justification reason (min 10 characters) is required and permanently recorded in audit logs. |
| **Timezone Consistency** | All dates and circulation clocks consistently use **India Standard Time (IST, UTC+05:30)** via injected `IDateTimeProvider`. |

---

## 2. Architecture & Database Schema

### Database Highlights
- **Engine**: Microsoft SQL Server
- **ORM**: Entity Framework Core 10 (Code-First)
- **Concurrency Control**: Optimistic concurrency tokens (`byte[] RowVersion` with `.IsRowVersion()`) on `BookCopy` and `Loan`.
- **Referential Integrity**: Restricted deletes (`DeleteBehavior.Restrict`) on all master data (Departments, Students, Categories, Publishers, Authors, Books, and Copies) to prevent accidental loss of historical circulation and financial audit records.
- **Unique Constraints**:
  - Filtered Unique Index on `Loans.BookCopyId` where `[Status] = 0` (Active).
  - Filtered Unique Index on `Books.ISBN` where `[ISBN] IS NOT NULL AND [ISBN] <> ''`.
  - Unique Index on `Departments.DepartmentCode`.
  - Unique Index on `Students.EnrollmentNumber`.
  - Unique Index on `BookCopies.AccessionNumber`.
  - Unique Index on `FinePayments.ReceiptNumber`.

---

## 3. Security & Access Control Matrix

| Controller / Action | Admin | Librarian | Anonymous |
| :--- | :---: | :---: | :---: |
| **Home/Dashboard** (`/`) | Full Access | Full Access | Redirect to Login |
| **Book Catalog** (`/Books`) | Full Access | Full Access | Redirect to Login |
| **Physical Inventory** (`/BookCopies`) | Full Access | Full Access | Redirect to Login |
| **Circulation (Issue/Return/Renew)** (`/Circulation`) | Full Access | Full Access | Redirect to Login |
| **Fine Collection & Receipts** (`/Fines`) | Full Access | Full Access | Redirect to Login |
| **Fine Waivers** (`/Fines/Waive`) | **Authorized** | *Forbidden (403)* | Redirect to Login |
| **Master Data (Dept/Author/Cat/Pub)** | Full Access | Full Access | Redirect to Login |
| **Operational Reports** (`/Reports`) | Full Access | Full Access | Redirect to Login |
| **Account Login/Logout** (`/Account`) | Accessible | Accessible | Accessible |

### Default Seeded Credentials
- **Administrator**: `admin@collegelibrary.edu` / `Admin@123456`
- **Librarian**: `librarian@collegelibrary.edu` / `Librarian@123456`

---

## 4. Automated Test Suite (40/40 Passing)

The project includes 40 comprehensive unit and integration tests across 6 dedicated test suites in `Tests/`:

```bash
dotnet test "d:\College Library Management System\Tests\Tests.csproj"
```

### Test Coverage Summary

1. **`Phase1FoundationTests.cs` (12 Tests)**
   - Default library policy settings validation.
   - Fine calculation logic: On-time return incurs ₹0 fine.
   - Fine calculation logic: 1-day overdue incurs ₹5, 5-days overdue incurs ₹25.
   - IST Timezone arithmetic and date boundary checks.
   - Enum mappings for `BookCopyStatus`, `LoanStatus`, `FineStatus`, `PaymentMethod`.

2. **`Phase2MasterDataTests.cs` (7 Tests)**
   - Department duplicate code rejection (case-insensitive).
   - Department deletion protection when students are assigned.
   - Student duplicate enrollment number rejection.
   - Student creation with non-existent department rejection.
   - Book catalog duplicate ISBN rejection.
   - Multi-author book associations and catalog search filters.

3. **`Phase3InventoryTests.cs` (5 Tests)**
   - Duplicate copy accession number rejection.
   - Sequential batch copy generation (`LIB-0101` to `LIB-0105`).
   - Transactional safety: batch creation rollbacks if any accession number already exists.
   - Protection against status modification when physical copy has an active loan.
   - Copy status update success when copy is not on loan.

4. **`Phase4CirculationTests.cs` (7 Tests)**
   - Maximum 3 active books limit per student enforced.
   - Borrowing blocked when student has any overdue unreturned loan.
   - Return on schedule incurs exactly ₹0.00 fine and marks copy Available.
   - Return 4 days overdue calculates ₹20.00 fine (`4 days * ₹5.00/day`) and generates Pending `Fine`.
   - Eligible active loan renewed for an additional 14 days.
   - Second renewal attempt rejected (max 1 renewal rule).
   - Overdue loan renewal attempt rejected (must return overdue book).

5. **`Phase5FinePaymentTests.cs` (5 Tests)**
   - Exact balance payment succeeds, marks fine `Paid`, and generates `REC-yyyyMMdd-XXXXXX` receipt.
   - Partial payment attempt is strictly rejected (policy violation).
   - Duplicate payment attempt on already paid fine is rejected.
   - Administrative fine waiver with valid justification succeeds and preserves audit trail.
   - Fine waiver attempt with empty or short justification (<10 chars) is rejected.

6. **`Phase6ReportTests.cs` (4 Tests)**
   - Real-time aggregate Dashboard metrics calculation.
   - Overdue loans operational report with department and days overdue filtering.
   - Inventory report summarizing copies across categories and departments.
   - Financial collections report revenue calculations by payment method.

---

## 5. Running the Application Locally

### Prerequisites
- .NET 10 SDK (`dotnet --version` outputting `10.0.x`)
- Microsoft SQL Server (LocalDB or MSSQLSERVER localhost)

### Configuration
Connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CollegeLibraryDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
  }
}
```

### Run Commands
```powershell
# 1. Run all tests
dotnet test "d:\College Library Management System\Tests\Tests.csproj"

# 2. Launch web application
cd "d:\College Library Management System\College Library Management System"
dotnet run
```

Application will be available at:
- `http://localhost:5068`
- `https://localhost:7256`
