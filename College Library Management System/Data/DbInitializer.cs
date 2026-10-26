using College_Library_Management_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace College_Library_Management_System.Data
{
    public static class DbInitializer
    {
        public const string AdminRole = "Admin";
        public const string LibrarianRole = "Librarian";
        public const string StudentRole = "Student";

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

            // Apply migrations automatically if pending
            try
            {
                if (context.Database.IsSqlServer())
                {
                    await context.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database.");
                throw;
            }

            // 1. Seed Roles
            string[] roles = [AdminRole, LibrarianRole, StudentRole];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Role '{Role}' created.", role);
                }
            }

            // 2. Seed Default Admin User
            const string adminEmail = "admin@collegelibrary.edu";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Chief Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, AdminRole);
                    logger.LogInformation("Admin user created with email '{Email}'.", adminEmail);
                }
                else
                {
                    logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // 3. Seed Default Librarian User
            const string librarianEmail = "librarian@collegelibrary.edu";
            var librarianUser = await userManager.FindByEmailAsync(librarianEmail);
            if (librarianUser == null)
            {
                librarianUser = new ApplicationUser
                {
                    UserName = librarianEmail,
                    Email = librarianEmail,
                    FullName = "College Librarian",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(librarianUser, "Librarian@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(librarianUser, LibrarianRole);
                    logger.LogInformation("Librarian user created with email '{Email}'.", librarianEmail);
                }
                else
                {
                    logger.LogError("Failed to create librarian user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // 4. Seed Library Settings
            if (!await context.LibrarySettings.AnyAsync())
            {
                var settings = new LibrarySetting
                {
                    LoanPeriodDays = 14,
                    DailyFineRate = 5.00m,
                    MaxBooksPerStudent = 3,
                    BlockIssueWhenOverdue = true,
                    AllowRenewal = true,
                    MaxRenewalsPerLoan = 1,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = adminUser?.Id
                };

                context.LibrarySettings.Add(settings);
                await context.SaveChangesAsync();
                logger.LogInformation("Default LibrarySetting record seeded.");
            }

            // 5. Seed Departments if empty
            if (!await context.Departments.AnyAsync())
            {
                var departments = new List<Department>
                {
                    new() { DepartmentName = "Computer Science & Engineering", DepartmentCode = "CSE" },
                    new() { DepartmentName = "Information Technology", DepartmentCode = "IT" },
                    new() { DepartmentName = "Mechanical Engineering", DepartmentCode = "ME" },
                    new() { DepartmentName = "Electrical & Electronics Engineering", DepartmentCode = "EEE" },
                    new() { DepartmentName = "Civil Engineering", DepartmentCode = "CE" },
                    new() { DepartmentName = "Business Administration", DepartmentCode = "MBA" }
                };

                context.Departments.AddRange(departments);
                await context.SaveChangesAsync();
                logger.LogInformation("Initial departments seeded.");
            }

            // 6. Seed Categories if empty
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new() { Name = "Computer Science & Programming", Description = "Software engineering, algorithms, networks, and databases" },
                    new() { Name = "Mathematics & Statistics", Description = "Discrete mathematics, calculus, probability, and linear algebra" },
                    new() { Name = "Electrical & Electronics", Description = "Circuits, microprocessors, digital signal processing" },
                    new() { Name = "Mechanical Engineering", Description = "Thermodynamics, mechanics, fluid dynamics, and robotics" },
                    new() { Name = "Management & Leadership", Description = "Organizational behavior, project management, and strategy" },
                    new() { Name = "General Science & Reference", Description = "Encyclopedias, physics, chemistry, and research manuals" }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
                logger.LogInformation("Initial categories seeded.");
            }

            // 7. Seed Publishers if empty
            if (!await context.Publishers.AnyAsync())
            {
                var publishers = new List<Publisher>
                {
                    new() { Name = "Pearson Education", Address = "London, United Kingdom", ContactEmail = "contact@pearson.com" },
                    new() { Name = "McGraw-Hill Education", Address = "New York, USA", ContactEmail = "info@mheducation.com" },
                    new() { Name = "O'Reilly Media", Address = "Sebastopol, California, USA", ContactEmail = "support@oreilly.com" },
                    new() { Name = "Cambridge University Press", Address = "Cambridge, UK", ContactEmail = "academic@cambridge.org" },
                    new() { Name = "MIT Press", Address = "Cambridge, Massachusetts, USA", ContactEmail = "mitpress-orders@mit.edu" }
                };

                context.Publishers.AddRange(publishers);
                await context.SaveChangesAsync();
                logger.LogInformation("Initial publishers seeded.");
            }

            // 8. Seed Authors if empty
            if (!await context.Authors.AnyAsync())
            {
                var authors = new List<Author>
                {
                    new() { Name = "Andrew S. Tanenbaum", Biography = "Professor Emeritus of Computer Science at Vrije Universiteit Amsterdam, known for MINIX and books on Operating Systems and Computer Networks." },
                    new() { Name = "Robert C. Martin", Biography = "Software engineer and author, also known as 'Uncle Bob', widely recognized for Clean Code and Agile Software Development." },
                    new() { Name = "Thomas H. Cormen", Biography = "Co-author of 'Introduction to Algorithms' and Professor of Computer Science at Dartmouth College." },
                    new() { Name = "Donald E. Knuth", Biography = "Professor Emeritus at Stanford University and author of the multi-volume work 'The Art of Computer Programming'." },
                    new() { Name = "Martin Fowler", Biography = "Software developer, author, and international speaker on software design, refactoring, and enterprise application architecture." }
                };

                context.Authors.AddRange(authors);
                await context.SaveChangesAsync();
                logger.LogInformation("Initial authors seeded.");
            }

            // 9. Synchronize existing students without user accounts
            var unlinkedStudents = await context.Students
                .Where(s => s.UserId == null)
                .ToListAsync();

            foreach (var student in unlinkedStudents)
            {
                var email = !string.IsNullOrWhiteSpace(student.Email) 
                    ? student.Email.Trim().ToLowerInvariant() 
                    : $"{student.EnrollmentNumber.ToLowerInvariant()}@student.collegelibrary.edu";

                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser == null)
                {
                    existingUser = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = student.FullName,
                        EmailConfirmed = true,
                        IsActive = student.IsActive,
                        CreatedAt = DateTime.UtcNow
                    };
                    var createResult = await userManager.CreateAsync(existingUser, "Student@123");
                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(existingUser, StudentRole);
                        student.UserId = existingUser.Id;
                        logger.LogInformation("Created student user account for '{Email}'.", email);
                    }
                }
                else
                {
                    student.UserId = existingUser.Id;
                    if (!await userManager.IsInRoleAsync(existingUser, StudentRole))
                    {
                        await userManager.AddToRoleAsync(existingUser, StudentRole);
                    }
                }
            }

            if (unlinkedStudents.Any(s => s.UserId != null))
            {
                await context.SaveChangesAsync();
            }
        }
    }
}
