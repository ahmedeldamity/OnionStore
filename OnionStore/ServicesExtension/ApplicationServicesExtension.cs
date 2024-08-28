﻿using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using DotNetCore_ECommerce.Helpers;
using Repository;
using Service;
using Shared.EmailSetting;

namespace API.ServicesExtension;
public static class ApplicationServicesExtension
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register Unit Of Work
        services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));

        // Register EmailSettings
        services.AddTransient(typeof(IEmailSettings), typeof(EmailSettings));

        // Register AuthService
        services.AddScoped(typeof(IAuthService), typeof(AuthService));

        // Register Product Service
        services.AddScoped(typeof(IProductService), typeof(ProductService));

        // Register Category Service
        services.AddScoped(typeof(ICategoryService), typeof(CategoryService));

        // Register Brand Service
        services.AddScoped(typeof(IBrandService), typeof(BrandService));

        // Register Basket Repository
        services.AddScoped(typeof(IBasketRepository), typeof(BasketRepository));

        // Register Order Service
        services.AddScoped(typeof(IOrderService), typeof(OrderService));

        // Register Payment Service
        services.AddScoped(typeof(IPaymentService), typeof(PaymentService));

        // Register Delivery Method Service
        services.AddScoped(typeof(IDeliveryMethodService), typeof(DeliveryMethodService));

        // Register Basket Service
        services.AddScoped(typeof(IBasketService), typeof(BasketService));

        // Register Account Service 
        services.AddScoped(typeof(IAccountService), typeof(AccountService));

        // --- Two Ways To Register AutoMapper
        // - First (harder)
        //builder.Services.AddAutoMapper(M => M.AddProfile(new MappingProfiles()));
        // - Second (easier)
        services.AddAutoMapper(typeof(MappingProfiles));

        return services;
    }
}