using FluentValidation;
using HomeServices.Application.Dtos;

namespace HomeServices.Application.Validators;

/// <summary>
/// FluentValidation rules for the DTOs that back the create/edit forms. Keeps
/// validation declarative and server-side; client-side unobtrusive validation is
/// derived from these attributes where possible.
/// </summary>

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).WithMessage("نام دسته‌بندی الزامی است (حداکثر ۱۵۰ کاراکتر).");
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200).Matches("^[a-z0-9-]*$")
            .WithMessage("اسلاگ فقط شامل حروف کوچک انگلیسی، عدد و خط تیره باشد.");
    }
}

public class CreateServiceDtoValidator : AbstractValidator<CreateServiceDto>
{
    public CreateServiceDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200).WithMessage("عنوان خدمت الزامی است.");
        RuleFor(x => x.Slug).MaximumLength(250).Matches("^[a-z0-9-]*$")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("اسلاگ معتبر نیست.");
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("دسته‌بندی را انتخاب کنید.");
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0).When(x => x.BasePrice.HasValue);
        RuleFor(x => x.EstimatedDurationMinutes).GreaterThan(0).When(x => x.EstimatedDurationMinutes.HasValue);
    }
}

public class CreateServiceRequestDtoValidator : AbstractValidator<CreateServiceRequestDto>
{
    public CreateServiceRequestDtoValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("دسته‌بندی را انتخاب کنید.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200).WithMessage("عنوان درخواست الزامی است.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000).WithMessage("توضیحات درخواست الزامی است.");
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City != null);
        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address != null);
        RuleFor(x => x.BudgetMin).LessThanOrEqualTo(x => x.BudgetMax).When(x => x.BudgetMin.HasValue && x.BudgetMax.HasValue)
            .WithMessage("حداقل بودجه نمی‌تواند بیشتر از حداکثر بودجه باشد.");
    }
}

public class CreateProposalDtoValidator : AbstractValidator<CreateProposalDto>
{
    public CreateProposalDtoValidator()
    {
        RuleFor(x => x.RequestId).GreaterThan(0).WithMessage("درخواست معتبر نیست.");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("قیمت پیشنهادی باید بزرگتر از صفر باشد.");
        RuleFor(x => x.Message).MaximumLength(2000).When(x => x.Message != null);
    }
}

public class CreateReviewDtoValidator : AbstractValidator<CreateReviewDto>
{
    public CreateReviewDtoValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("امتیاز بین ۱ تا ۵ باشد.");
        RuleFor(x => x.Comment).MaximumLength(2000).When(x => x.Comment != null);
        RuleFor(x => x.Professionalism).InclusiveBetween(1, 5).When(x => x.Professionalism.HasValue);
        RuleFor(x => x.Punctuality).InclusiveBetween(1, 5).When(x => x.Punctuality.HasValue);
        RuleFor(x => x.Quality).InclusiveBetween(1, 5).When(x => x.Quality.HasValue);
        RuleFor(x => x.Responsiveness).InclusiveBetween(1, 5).When(x => x.Responsiveness.HasValue);
        RuleFor(x => x.Value).InclusiveBetween(1, 5).When(x => x.Value.HasValue);
    }
}

public class CreateExpertProfileDtoValidator : AbstractValidator<CreateExpertProfileDto>
{
    public CreateExpertProfileDtoValidator()
    {
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(200).WithMessage("نام کسب‌و‌کار الزامی است.");
        RuleFor(x => x.Bio).MaximumLength(2000).When(x => x.Bio != null);
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City != null);
    }
}

public class UpsertSiteSettingDtoValidator : AbstractValidator<UpsertSiteSettingDto>
{
    public UpsertSiteSettingDtoValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Value).MaximumLength(2000).When(x => x.Value != null);
    }
}
