﻿using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Service.ConfigurationData;

namespace API.Health;
public class MailHealthCheck(IOptions<MailData> mailData) : IHealthCheck
{
    private readonly MailData _mailData = mailData.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var smtp = new SmtpClient();

            smtp.Connect(_mailData.Host, _mailData.Port, SecureSocketOptions.SslOnConnect, cancellationToken);

            smtp.Authenticate(_mailData.Email, _mailData.Password, cancellationToken);

            return await Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (Exception exception)
        {
            return await Task.FromResult(HealthCheckResult.Unhealthy(exception: exception));
        }
    }
}