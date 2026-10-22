/*using System.Security.Claims;
using Authentication.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Platform.EntityFrameworkCore;

namespace Authentication.Api;

internal static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/auth");

        auth.MapPost("/register", RegisterAsync)
            .AllowAnonymous();

        auth.MapPost("/login", LoginAsync)
            .AllowAnonymous();

        auth.MapGet("/me", GetCurrentUserAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        AuthenticationService service,
        CancellationToken cancellationToken)
    {
        var created = await service.RegisterAsync(
            request.Email, request.Password, cancellationToken);

        return created
            ? Results.StatusCode(StatusCodes.Status201Created)
            : Results.BadRequest(new { error = "Некорректные данные или пользователь уже существует." });
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        AuthenticationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.LoginAsync(
            request.Email, request.Password, cancellationToken);

        return result is null
            ? Results.Unauthorized()
            : Results.Ok(result);
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(idValue, out var id))
            return Results.Unauthorized();

        var user = await dbContext.Set<AuthUser>()
            .Where(item => item.Id == id && !item.IsBlocked)
            .Select(item => new CurrentUserResponse(
                item.Id, item.Email, item.Role.ToString()))
            .SingleOrDefaultAsync(cancellationToken);

        return user is null
            ? Results.Unauthorized()
            : Results.Ok(user);
    }
}*/