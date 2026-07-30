using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeServices.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the main database with the initial baseline data: site settings (branding),
/// the Angi-style category tree (Interior/Exterior/Lawn&amp;Garden/Other), sample services
/// under each category, a sample approved expert with portfolio, and one sample request
/// → proposal → order → review chain so the UI has data to render on first run.
/// Idempotent: checks for existing data before inserting.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, ILogger? logger = null)
    {
        logger?.LogInformation("Applying database migrations and seeding baseline data...");

        try
        {
            await context.Database.MigrateAsync();
            await SeedSiteSettingsAsync(context, logger);
            await SeedCategoriesAsync(context, logger);
            await SeedServicesAsync(context, logger);
            await SeedExpertAsync(context, logger);
            await SeedSampleWorkflowAsync(context, logger);
            logger?.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    // ---------------------------------------------------------------- Site settings
    private static async Task SeedSiteSettingsAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.SiteSettings.AnyAsync()) return;

        logger?.LogInformation("Seeding site settings...");
        var settings = new List<SiteSetting>
        {
            New("Site.Name", "خدمات منزل", "Branding", "نام نمایشی سایت", 1),
            New("Site.Tagline", "کارشناسان مورد اعتماد برای خانه شما", "Branding", "شعار سایت", 2),
            New("Site.LogoUrl", "/uploads/site/logo.svg", "Branding", "لوگوی سایت", 3),
            New("Site.FaviconUrl", "/uploads/site/favicon.ico", "Branding", "فاوآیکن", 4),
            New("Site.BannerUrl", "/uploads/site/hero.jpg", "Branding", "تصویر هدر صفحه اصلی", 5),
            New("Theme.PrimaryColor", "#FC5647", "Theme", "رنگ اصلی برند (نارنجی غروب Angi)", 10),
            New("Theme.SecondaryColor", "#0F7B6C", "Theme", "رنگ ثانویه (سبز عمیق)", 11),
            New("Theme.BackgroundColor", "#FBF7F4", "Theme", "رنگ پس‌زمینه (Foam)", 12),
            New("Theme.TextColor", "#1B2A41", "Theme", "رنگ متن (Prussian Navy)", 13),
            New("Hero.Title", "کارشناسان باکیفیت برای خانه‌تان پیدا کنید", "Hero", "عنوان بخش هرو", 20),
            New("Hero.Subtitle", "درخواست خود را ثبت کنید، پیشنهاد کارشناسان را مقایسه کنید و بهترین را انتخاب کنید.", "Hero", "زیرعنوان هرو", 21),
            New("Contact.Phone", "021-12345678", "Contact", "تلفن تماس", 30),
            New("Contact.Email", "support@homeservices.ir", "Contact", "ایمیل پشتیبانی", 31),
            New("Contact.Address", "تهران، ایران", "Contact", "آدرس", 32),
            New("Social.Instagram", "https://instagram.com/homeservices", "Social", "اینستاگرام", 40),
            New("Social.Telegram", "https://t.me/homeservices", "Social", "تلگرام", 41),
        };

        await context.SiteSettings.AddRangeAsync(settings);
        await context.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- Categories
    private static async Task SeedCategoriesAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Categories.AnyAsync()) return;

        logger?.LogInformation("Seeding categories (Angi-style groups)...");

        var categories = new List<Category>
        {
            // Interior
            Cat("نظافت منزل", "house-cleaning", CategoryGroup.Interior, "نظافت دوره‌ای، عمیق و پایان کار", 1, "🧹"),
            Cat("لوله‌کشی", "plumbing", CategoryGroup.Interior, "تعمیر و نصب تأسیسات و لوله‌کشی", 2, "🚰"),
            Cat("برق‌کاری", "electrical", CategoryGroup.Interior, "تعمیر و نصب تأسیسات برقی", 3, "💡"),
            Cat("تهویه و سرمایش (HVAC)", "hvac", CategoryGroup.Interior, "کولر، پکیج، چیلر و سیستم‌های گرمایشی", 4, "❄️"),
            Cat("نقاشی داخلی", "interior-painting", CategoryGroup.Interior, "رنگ‌آمیزی دیوار و سقف داخلی", 5, "🎨"),
            Cat("کف‌پوش و پارکت", "flooring", CategoryGroup.Interior, "نصب و تعمیر سرامیک، پارکت و موکت", 6, "🔲"),
            Cat("نصب و تعمیر لوازم خانگی", "appliance-repair", CategoryGroup.Interior, "تعمیر یخچال، ماشین لباسشویی و...", 7, "🔌"),
            // Exterior
            Cat("سقف و عایق", "roofing", CategoryGroup.Exterior, "تعمیر و عایق‌بندی سقف", 8, "🏠"),
            Cat("نقاشی نمای بیرونی", "exterior-painting", CategoryGroup.Exterior, "رنگ‌آمیزی نما و بیرون ساختمان", 9, "🖌️"),
            Cat("نما و نمای سنگی", "siding", CategoryGroup.Exterior, "تعمیر و نصب نما", 10, "🏢"),
            // Lawn & Garden
            Cat("باغبانی و فضای سبز", "landscaping", CategoryGroup.LawnGarden, "طراحی و نگهداری باغ و محوطه", 11, "🌳"),
            Cat("هرس و نگهداری درخت", "tree-service", CategoryGroup.LawnGarden, "هرس، جابجایی و نگهداری درختان", 12, "🌲"),
            Cat("سم‌پاشی و کنترل آفات", "pest-control", CategoryGroup.LawnGarden, "سم‌پاشی منزل و باغ، کنترل آفات", 13, "🐛"),
            // Other
            Car("دستیار عمومی (هندمن)", "handyman", CategoryGroup.Other, "تعمیرات کوچک و کارهای فنی عمومی", 14, "🛠️"),
            Cat("نظافت فرش و موکت", "carpet-cleaning", CategoryGroup.Other, "شستشوی فرش، موکت و مبلمان", 15, "🧼"),
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- Services
    private static async Task SeedServicesAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Services.AnyAsync()) return;

        logger?.LogInformation("Seeding sample services...");

        var plumbingId = await context.Categories.Where(c => c.Slug == "plumbing").Select(c => c.Id).FirstAsync();
        var electricalId = await context.Categories.Where(c => c.Slug == "electrical").Select(c => c.Id).FirstAsync();
        var cleaningId = await context.Categories.Where(c => c.Slug == "house-cleaning").Select(c => c.Id).FirstAsync();
        var hvacId = await context.Categories.Where(c => c.Slug == "hvac").Select(c => c.Id).FirstAsync();
        var paintingId = await context.Categories.Where(c => c.Slug == "interior-painting").Select(c => c.Id).FirstAsync();
        var handymanId = await context.Categories.Where(c => c.Slug == "handyman").Select(c => c.Id).FirstAsync();

        var services = new List<Service>
        {
            Svc("تعمیر نشتی لوله", "leak-repair", "تعمیر و رفع نشتی لوله‌های آب", plumbingId, 150000, 60, true, 1),
            Svc("نصب و تعمیر پمپ آب", "water-heater-install", "نصب، تعویض و تعمیر پمپ و آبگرمکن", plumbingId, 350000, 120, true, 2),
            C("تعویض شیرآلات", "faucet-repair", "تعویض و تعمیر شیرآلات آشپزخانه و حمام", plumbingId, 120000, 45, 3),

            Svc("تعمیر قطعات برقی", "electrical-repair", "تعمیر خرابی‌های برقی و پریز و کلید", electricalId, 180000, 60, true, 1),
            Svc("نصب چراغ و لوستر", "light-install", "نصب چراغ، لوستر و تجهیزات روشنایی", electricalId, 200000, 90, true, 2),

            Svc("نظافت دوره‌ای منزل", "housekeeping-periodic", "نظافت کامل دوره‌ای آپارتمان", cleaningId, 450000, 240, true, 1),
            Svc("نظافت پایان کار", "post-construction-cleaning", "نظافت پس از ساخت و بازسازی", cleaningId, 900000, 480, false, 2),

            Svc("تعمیر و شارژ کولر", "ac-service", "شارژ گاز، شستشو و تعمیر کولر", hvacId, 400000, 120, true, 1),
            Svc("سرویس پکیج", "boiler-service", "سرویس دوره‌ای پکیج و رادیاتور", hvacId, 350000, 90, true, 2),

            Svc("رنگ‌آمیزی اتاق", "room-painting", "رنگ‌آمیزی دیوار و سقف یک اتاق", paintingId, 600000, 360, false, 1),

            Svc("تعمیرات کوچک منزل", "small-repairs", "تعمیرات فنی عمومی و کارهای کوچک", handymanId, 130000, 60, true, 1),
        };

        await context.Services.AddRangeAsync(services);
        await context.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- Sample expert
    private static async Task SeedExpertAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.ExpertProfiles.AnyAsync()) return;

        logger?.LogInformation("Seeding sample expert profile...");

        var plumbingId = await context.Categories.Where(c => c.Slug == "plumbing").Select(c => c.Id).FirstAsync();
        var hvacId = await context.Categories.Where(c => c.Slug == "hvac").Select(c => c.Id).FirstAsync();

        // A fixed sample expert user id (the matching user is created in the Identity service seed).
        var expertUserId = new Guid("A1B2C3D4-E5F6-7890-1234-567890ABCDEF");

        var expert = new ExpertProfile
        {
            UserId = expertUserId,
            BusinessName = "تأسیسات برتر",
            Bio = "با بیش از ۱۲ سال تجربه در لوله‌کشی و سیستم‌های گرمایشی، آماده ارائه بهترین خدمات به مشتریان هستیم.",
            LogoUrl = "/uploads/experts/expert1-logo.png",
            CoverImageUrl = "/uploads/experts/expert1-cover.jpg",
            ServiceArea = "تهران - شمال و مرکز",
            City = "تهران",
            BusinessHours = "شنبه تا پنج‌شنبه ۸:۰۰ تا ۲۰:۰۰",
            IsVerified = true,
            IsApproved = true,
            RatingAverage = 4.8,
            ReviewCount = 1,
            JobsCompleted = 1,
            ResponseTimeMinutes = 30,
            JoinedAt = DateTime.UtcNow.AddDays(-200),
            IsActive = true,
            ExpertCategories = new List<ExpertCategory>
            {
                new() { CategoryId = plumbingId },
                new() { CategoryId = hvacId },
            },
            PortfolioImages = new List<ExpertPortfolioImage>
            {
                new() { ImageUrl = "/uploads/experts/work1.jpg", ThumbnailUrl = "/uploads/experts/work1-thumb.jpg", Title = "تعویض لوله‌کشی ساختمان", DisplayOrder = 1 },
                new() { ImageUrl = "/uploads/experts/work2.jpg", ThumbnailUrl = "/uploads/experts/work2-thumb.jpg", Title = "نصب پکیج", DisplayOrder = 2 },
            }
        };

        await context.ExpertProfiles.AddAsync(expert);
        await context.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- Sample workflow
    private static async Task SeedSampleWorkflowAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.ServiceRequests.AnyAsync()) return;

        logger?.LogInformation("Seeding sample request -> proposal -> order -> review...");

        var plumbingId = await context.Categories.Where(c => c.Slug == "plumbing").Select(c => c.Id).FirstAsync();
        var leakServiceId = await context.Services.Where(s => s.Slug == "leak-repair").Select(s => s.Id).FirstOrDefaultAsync();
        var expert = await context.ExpertProfiles.FirstAsync();

        var customerId = new Guid("11111111-1111-1111-1111-111111111111");
        var expertId = expert.UserId;

        var request = new ServiceRequest
        {
            CustomerId = customerId,
            CategoryId = plumbingId,
            ServiceId = leakServiceId,
            Title = "نشتی لوله زیر سینک ظرفشویی",
            Description = "از زیر سینک ظرفشویی آپارتمان نشتی آب دارم. به نظر می‌رسه از اتصالات باشد. نیاز به بررسی و تعمیر فوری.",
            Address = "تهران، ولنجک، خیابان اول پارک",
            City = "تهران",
            ZipCode = "19847",
            Urgency = UrgencyLevel.Within24Hours,
            PreferredDate = DateTime.UtcNow.AddDays(2),
            BudgetMin = 100000,
            BudgetMax = 300000,
            IsHomeOwner = true,
            Status = RequestStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            CreatedBy = customerId,
        };

        var proposal = new Proposal
        {
            Request = request,
            ExpertId = expertId,
            Price = 180000,
            EstimatedDurationHours = 1,
            Message = "با سلام، بررسی نشتی و تعمیر اتصالات با قطعات اصلی. ضمانت یک ماهه.",
            AvailableStartDate = DateTime.UtcNow.AddDays(-9),
            Status = ProposalStatus.Accepted,
            CreatedAt = DateTime.UtcNow.AddDays(-9),
            CreatedBy = expertId,
        };

        var order = new Order
        {
            Request = request,
            Proposal = proposal,
            CustomerId = customerId,
            ExpertId = expertId,
            OrderNumber = "HS-100001",
            Status = OrderStatus.Completed,
            TotalAmount = 180000,
            ScheduledDate = DateTime.UtcNow.AddDays(-7),
            CompletedDate = DateTime.UtcNow.AddDays(-6),
            Notes = "کار با کیفیت انجام شد.",
            CreatedAt = DateTime.UtcNow.AddDays(-9),
            CreatedBy = customerId,
        };

        var review = new Review
        {
            Order = order,
            Request = request,
            CustomerId = customerId,
            ExpertId = expertId,
            Rating = 5,
            Professionalism = 5,
            Punctuality = 4,
            Quality = 5,
            Responsiveness = 5,
            Value = 4,
            Comment = "خیلی حرفه‌ای و سریع کارشون رو انجام دادن. حتماً پیشنهاد می‌کنم.",
            IsVerified = true,
            Status = ReviewStatus.Approved,
            ServiceDate = DateTime.UtcNow.AddDays(-6),
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            CreatedBy = customerId,
        };

        var payment = new Payment
        {
            Order = order,
            Amount = 180000,
            PaymentMethod = PaymentMethod.Online,
            Status = PaymentStatus.Succeeded,
            TransactionId = "TXN-SEED-0001",
            PaidAt = DateTime.UtcNow.AddDays(-6),
        };

        context.ServiceRequests.Add(request);
        context.Proposals.Add(proposal);
        context.Orders.Add(order);
        context.Payments.Add(payment);
        context.Reviews.Add(review);

        await context.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- Helpers
    private static SiteSetting New(string key, string value, string group, string desc, int order) => new()
    {
        Key = key, Value = value, Group = group, Description = desc, DisplayOrder = order
    };

    private static Category Cat(string name, string slug, CategoryGroup group, string desc, int order, string icon) => new()
    {
        Name = name, Slug = slug, Group = group, Description = desc, DisplayOrder = order, IconUrl = icon, IsActive = true
    };

    private static Category Car(string name, string slug, CategoryGroup group, string desc, int order, string icon) => new()
    {
        Name = name, Slug = slug, Group = group, Description = desc, DisplayOrder = order, IconUrl = icon, IsActive = true
    };

    private static Service Svc(string title, string slug, string desc, int categoryId, decimal price, int minutes, bool fixedPrice, int order) => new()
    {
        Title = title, Slug = slug, Description = desc, CategoryId = categoryId, BasePrice = price, EstimatedDurationMinutes = minutes, IsFixedPrice = fixedPrice, DisplayOrder = order, IsActive = true
    };

    private static Service C(string title, string slug, string desc, int categoryId, decimal price, int minutes, int order) => new()
    {
        Title = title, Slug = slug, Description = desc, CategoryId = categoryId, BasePrice = price, EstimatedDurationMinutes = minutes, IsFixedPrice = true, DisplayOrder = order, IsActive = true
    };
}
