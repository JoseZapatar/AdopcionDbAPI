namespace AdopcionDbAPI.Services.Email;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
