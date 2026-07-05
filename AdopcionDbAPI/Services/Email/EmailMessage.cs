namespace AdopcionDbAPI.Services.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string Body
);
