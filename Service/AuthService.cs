﻿using Core.Dtos;
using Core.Entities.IdentityEntities;
using Core.ErrorHandling;
using Core.Interfaces.Services;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Repository.Store;
using Service.ConfigurationData;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Service;
public class AuthService(IOptions<JWTData> jWTData, UserManager<AppUser> _userManager, StoreContext _storeContext, IEmailSettingService _emailSettings) : IAuthService
{
    private readonly JWTData _jWTData = jWTData.Value;

    public async Task<Result> SendEmailVerificationCode(ClaimsPrincipal User)
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        if (await _userManager.FindByEmailAsync(userEmail!) is not { } user)
            return Result.Success();

        if (user.EmailConfirmed)
            return Result.Failure(new Error(400, "Your email is already confirmed."));

        var code = GenerateSecureCode();

        var subject = $"✅ {userEmail!.Split('@')[0]}, Your pin code is {code}. \r\nPlease confirm your email address";

        var body = EmailBody(code, userEmail.Split('@')[0], "Email Verification", "Thank you for registering with our service. To complete your registration");

        EmailResponse emailToSend = new(subject, body, userEmail);

        await _storeContext.IdentityCodes.AddAsync(new IdentityCode()
        {
            Code = HashCode(code),
            IsActive = true,
            User = user,
            AppUserId = user.Id,
            ForRegisterationConfirmed = true
        });

        await _storeContext.SaveChangesAsync();

        BackgroundJob.Enqueue(() => _emailSettings.SendEmailMessage(emailToSend));

        return Result.Success();
    }

    public async Task<Result> SendEmailVerificationCodeV2(ClaimsPrincipal User)
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        if (await _userManager.FindByEmailAsync(userEmail!) is not { } user)
            return Result.Success("If your email is registered with us, a email verification code has been successfully sent.");

        if (user.EmailConfirmed)
            return Result.Failure(new Error(400, "Your email is already confirmed."));

        var code = GenerateSecureCode();

        var subject = $"✅ {userEmail!.Split('@')[0]}, Your pin code is {code}. \r\nPlease confirm your email address";

        var body = LoadEmailTemplate("Templates/EmailTemplate.html", code, user.DisplayName, "Reset Password", "You have requested to reset your password.");

        EmailResponse emailToSend = new(subject, body, userEmail);

        await _storeContext.IdentityCodes.AddAsync(new IdentityCode()
        {
            Code = HashCode(code),
            IsActive = true,
            User = user,
            AppUserId = user.Id,
            ForRegisterationConfirmed = true
        });

        await _storeContext.SaveChangesAsync();

        BackgroundJob.Enqueue(() => _emailSettings.SendEmailMessage(emailToSend));

        return Result.Success("If your email is registered with us, a email verification code has been successfully sent.");
    }

    public async Task<Result> VerifyRegisterCode(CodeVerificationRequest model, ClaimsPrincipal User)
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        if (await _userManager.FindByEmailAsync(userEmail!) is not { } user)
            return Result.Failure(new Error(400, "No account found with the provided email address."));

        var identityCode = await _storeContext.IdentityCodes
                            .Where(P => P.AppUserId == user.Id && P.ForRegisterationConfirmed)
                            .OrderBy(d => d.CreationTime)
                            .LastOrDefaultAsync();

        if (identityCode is null)
            return Result.Failure(new Error(400, "The reset code is missing or invalid. Please request a new reset code."));

        var lastCode = identityCode.Code;

        if (!ConstantComparison(lastCode, HashCode(model.VerificationCode)))
            return Result.Failure(new Error(400, "The reset code is missing or invalid. Please request a new reset code."));

        if (!identityCode.IsActive || identityCode.CreationTime.Minute + 5 < DateTime.UtcNow.Minute)
            return Result.Failure(new Error(400, "The reset code has either expired or is not active. Please request a new code."));

        identityCode.IsActive = false;

        _storeContext.IdentityCodes.Update(identityCode);

        user.EmailConfirmed = true;

        await _userManager.UpdateAsync(user);

        await _storeContext.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> SendPasswordResetEmail(EmailRequest email)
    {
        if (await _userManager.FindByEmailAsync(email.Email) is not { } user)
            return Result.Success();

        var code = GenerateSecureCode();

        var subject = $"✅ {user.DisplayName}, Reset Your Password - Verification Code: {code}";

        var body = EmailBody(code, user.DisplayName, "Reset Password", "You have requested to reset your password.");

        EmailResponse emailToSend = new(subject, body, email.Email);

        await _storeContext.IdentityCodes.AddAsync(new IdentityCode()
        {
            Code = HashCode(code),
            User = user,
            AppUserId = user.Id,
            ForRegisterationConfirmed = false,
        });

        await _storeContext.SaveChangesAsync();

        BackgroundJob.Enqueue(() => _emailSettings.SendEmailMessage(emailToSend));

        return Result.Success();
    }

    public async Task<Result> SendPasswordResetEmailV2(EmailRequest email)
    {
        if (await _userManager.FindByEmailAsync(email.Email) is not { } user)
            return Result.Success("If your email is registered with us, a password reset email has been successfully sent.");

        var code = GenerateSecureCode();

        var subject = $"✅ {user.DisplayName}, Reset Your Password - Verification Code: {code}";

        var body = LoadEmailTemplate("Templates/EmailTemplate.html", code, user.DisplayName, "Reset Password", "You have requested to reset your password.");

        EmailResponse emailToSend = new(subject, body, email.Email);

        await _storeContext.IdentityCodes.AddAsync(new IdentityCode()
        {
            Code = HashCode(code),
            User = user,
            AppUserId = user.Id,
            ForRegisterationConfirmed = false,
        });

        await _storeContext.SaveChangesAsync();

        BackgroundJob.Enqueue(() => _emailSettings.SendEmailMessage(emailToSend));

        return Result.Success("If your email is registered with us, a password reset email has been successfully sent.");
    }

    public async Task<Result> VerifyResetCode(CodeVerificationRequest model, ClaimsPrincipal User)
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        if (await _userManager.FindByEmailAsync(userEmail!) is not { } user)
            return Result.Failure(new Error(400, "No account associated with the provided email address was found, Please check the email and try again."));

        if (user.EmailConfirmed is false)
            return Result.Failure(new Error(400, "Please verify your email address before proceeding."));

        var identityCode = await _storeContext.IdentityCodes
                            .Where(P => P.AppUserId == user.Id && P.ForRegisterationConfirmed == false)
                            .OrderBy(d => d.CreationTime)
                            .LastOrDefaultAsync();

        if (identityCode is null)
            return Result.Failure(new Error(400, "The reset code is missing or invalid. Please request a new reset code."));

        if (identityCode.IsActive)
            return Result.Failure(new Error(400, "An active reset code already exists. Please use the existing code or wait until it expires to request a new one."));

        var lastCode = identityCode.Code;

        if (!ConstantComparison(lastCode, HashCode(model.VerificationCode)))
            return Result.Failure(new Error(400, "The reset code is missing or invalid. Please request a new reset code."));

        if (identityCode.CreationTime.Minute + 5 < DateTime.UtcNow.Minute)
            return Result.Failure(new Error(400, "The reset code has either expired or is not active. Please request a new code."));

        user.EmailConfirmed = true;

        identityCode.IsActive = true;

        identityCode.User = user;

        identityCode.ActivationTime = DateTime.UtcNow;

        _storeContext.IdentityCodes.Update(identityCode);

        await _userManager.UpdateAsync(user);

        await _storeContext.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ChangePassword(ChangePasswordRequest model, ClaimsPrincipal User)
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        if (await _userManager.FindByEmailAsync(userEmail!) is not { } user)
            return Result.Failure(new Error(400, "No account associated with the provided email address was found, Please check the email and try again."));

        if (user.EmailConfirmed is false) 
            return Result.Failure(new Error(400, "Please verify your email address before proceeding."));

        var identityCode = await _storeContext.IdentityCodes
                            .Where(p => p.AppUserId == user.Id && p.IsActive && p.ForRegisterationConfirmed == false)
                            .OrderByDescending(p => p.CreationTime)
                            .FirstOrDefaultAsync();

        if (identityCode is null)
            return Result.Failure(new Error(400, "The reset code is missing or invalid. Please request a new reset code."));

        var lastCode = identityCode.Code;

        if (!ConstantComparison(lastCode, HashCode(model.VerificationCode)))
            return Result.Failure(new Error(400, "The reset code is missing or invalid. Please request a new reset code."));

        if (identityCode.ActivationTime is null || identityCode.ActivationTime.Value.AddMinutes(30) < DateTime.UtcNow)
            return Result.Failure(new Error(400, "The reset code has either expired or is not active. Please request a new code."));

        using var transaction = await _storeContext.Database.BeginTransactionAsync();

        identityCode.IsActive = false;

        _storeContext.IdentityCodes.Update(identityCode);

        await _storeContext.SaveChangesAsync();

        var removePasswordResult = await _userManager.RemovePasswordAsync(user);

        if (removePasswordResult.Succeeded is false)
        {
            await transaction.RollbackAsync();

            var errors = string.Join(", ", removePasswordResult.Errors.Select(e => e.Description));

            return Result.Failure(new Error(400, errors));
        }

        var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);

        if (!addPasswordResult.Succeeded)
        {
            await transaction.RollbackAsync();

            var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));

            return Result.Failure(new Error(400, errors));
        }

        await transaction.CommitAsync();

        return Result.Success();
    }

    private string LoadEmailTemplate(string filePath, string code, string userName, string title, string message)
    {
        string template = File.ReadAllText(filePath);

        // Replace placeholders with actual values
        template = template.Replace("{{Code}}", code)
                           .Replace("{{UserName}}", userName)
                           .Replace("{{Title}}", title)
                           .Replace("{{Message}}", message)
                           .Replace("{{Year}}", DateTime.Now.Year.ToString());

        return template;
    }

    private string EmailBody(string code, string userName, string title, string message)
    {
        return $@"
                <!DOCTYPE html>
                <html lang=""en"">
                <head>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>Email Verification</title>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            line-height: 1.6;
                            background-color: #f5f5f5;
                            margin: 0;
                            padding: 0;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: auto;
                            padding: 20px;
                            background-color: #ffffff;
                            border-radius: 8px;
                            box-shadow: 0 0 10px rgba(0,0,0,0.1);
                        }}
                        .header {{
                            background-color: #007bff;
                            color: #ffffff;
                            padding: 10px;
                            text-align: center;
                            border-top-left-radius: 8px;
                            border-top-right-radius: 8px;
                        }}
                        .content {{
                            padding: 20px;
                        }}
                        .code {{
                            font-size: 24px;
                            font-weight: bold;
                            text-align: center;
                            margin-top: 20px;
                            margin-bottom: 30px;
                            color: #007bff;
                        }}
                        .footer {{
                            background-color: #f7f7f7;
                            padding: 10px;
                            text-align: center;
                            border-top: 1px solid #dddddd;
                            font-size: 12px;
                            color: #777777;
                        }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h2>{title}</h2>
                        </div>
                        <div class=""content"">
                            <p>Dear {userName},</p>
                            <p>{message}, please use the following verification code:</p>
                            <div class=""code"">{code}</div>
                            <p>This code will expire in 5 minutes. Please use it promptly to verify your email address.</p>
                            <p>If you did not request this verification, please ignore this email.</p>
                        </div>
                        <div class=""footer"">
                            <p>&copy; 2024 OnionStore. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";
    }

    private string GenerateSecureCode()
    {
        byte[] randomNumber = new byte[4];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        int result = BitConverter.ToInt32(randomNumber, 0);
        int positiveResult = Math.Abs(result);

        int sixDigitCode = positiveResult % 1000000;
        return sixDigitCode.ToString("D6");
    }

    private string HashCode(string code)
    {
        var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
        return BitConverter.ToString(hashedBytes).Replace("-", "");
    }

    private bool ConstantComparison(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }
}