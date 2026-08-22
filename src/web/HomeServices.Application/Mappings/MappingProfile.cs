using HomeServices.Application.Dtos;
using HomeServices.Domain.Entities;

namespace HomeServices.Application.Mappings;

/// <summary>
/// Central AutoMapper profile mapping domain entities to application DTOs. Most
/// maps are convention-based (same names); custom projections resolve category
/// names, counts and expert-aggregate fields that are not direct properties.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ----- Category -----
        CreateMap<Category, CategoryDto>()
            .ForMember(d => d.ParentName, o => o.MapFrom(s => s.Parent != null ? s.Parent.Name : null))
            .ForMember(d => d.SubCategoryCount, o => o.MapFrom(s => s.SubCategories.Count))
            .ForMember(d => d.ServiceCount, o => o.MapFrom(s => s.Services.Count));

        CreateMap<CreateCategoryDto, Category>();
        CreateMap<UpdateCategoryDto, Category>();

        // ----- Service & images -----
        CreateMap<ServiceImage, ServiceImageDto>();

        CreateMap<Service, ServiceDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));

        CreateMap<CreateServiceDto, Service>();
        CreateMap<UpdateServiceDto, Service>();

        // ----- Service request -----
        CreateMap<RequestImage, RequestImageDto>();

        CreateMap<ServiceRequest, ServiceRequestDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.CategoryIconUrl, o => o.MapFrom(s => s.Category != null ? s.Category.IconUrl : null))
            .ForMember(d => d.ServiceTitle, o => o.MapFrom(s => s.Service != null ? s.Service.Title : null))
            .ForMember(d => d.ProposalCount, o => o.MapFrom(s => s.Proposals.Count));

        CreateMap<CreateServiceRequestDto, ServiceRequest>();
        CreateMap<UpdateServiceRequestDto, ServiceRequest>();

        // ----- Proposal -----
        CreateMap<Proposal, ProposalDto>()
            .ForMember(d => d.RequestTitle, o => o.MapFrom(s => s.Request != null ? s.Request.Title : null));

        CreateMap<CreateProposalDto, Proposal>();
        CreateMap<UpdateProposalDto, Proposal>();

        // ----- Order -----
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.RequestTitle, o => o.MapFrom(s => s.Request != null ? s.Request.Title : null));

        // ----- Review -----
        CreateMap<Review, ReviewDto>();

        CreateMap<CreateReviewDto, Review>();

        // ----- Expert profile & portfolio -----
        CreateMap<ExpertPortfolioImage, ExpertPortfolioImageDto>();

        CreateMap<ExpertProfile, ExpertProfileDto>()
            .ForMember(d => d.CategoryIds,
                o => o.MapFrom(s => s.ExpertCategories.Select(ec => ec.CategoryId).ToList()))
            .ForMember(d => d.CategoryNames,
                o => o.MapFrom(s => s.ExpertCategories
                    .Select(ec => ec.Category != null ? ec.Category.Name : string.Empty).ToList()));

        // Ignore collection navigations on create/update — they are managed explicitly in code.
        CreateMap<CreateExpertProfileDto, ExpertProfile>()
            .ForMember(d => d.ExpertCategories, o => o.Ignore())
            .ForMember(d => d.PortfolioImages, o => o.Ignore());
        CreateMap<UpdateExpertProfileDto, ExpertProfile>()
            .ForMember(d => d.ExpertCategories, o => o.Ignore())
            .ForMember(d => d.PortfolioImages, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore());

        // ----- Site setting -----
        CreateMap<SiteSetting, SiteSettingDto>();
        CreateMap<UpsertSiteSettingDto, SiteSetting>();

        // ----- Media -----
        CreateMap<Media, MediaDto>();

        // ----- Support workflow -----
        CreateMap<PaymentVerificationReport, PaymentVerificationReportDto>()
            .ForMember(d => d.OrderNumber, o => o.MapFrom(s => s.Order != null ? s.Order.OrderNumber : ""));

        CreateMap<WorkCompletionReport, WorkCompletionReportDto>()
            .ForMember(d => d.OrderNumber, o => o.MapFrom(s => s.Order != null ? s.Order.OrderNumber : ""))
            .ForMember(d => d.OrderAmount, o => o.MapFrom(s => s.Order != null ? s.Order.TotalAmount : 0));

        CreateMap<WorkCompletionAttachment, WorkCompletionAttachmentDto>();

        CreateMap<ExpertPayout, ExpertPayoutDto>();

        // ----- Support ticketing -----
        CreateMap<SupportTicket, SupportTicketDto>()
            .ForMember(d => d.OrderNumber, o => o.MapFrom(s => s.Order != null ? s.Order.OrderNumber : null))
            .ForMember(d => d.MessageCount, o => o.MapFrom(s => s.Messages.Count));

        CreateMap<SupportTicketMessage, SupportTicketMessageDto>();
        CreateMap<SupportTicketAttachment, SupportTicketAttachmentDto>();
    }
}
