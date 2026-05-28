using Cilt_Beninden_Kanser_Api.Application.Interfaces.Repositories;
using DomainUser = Cilt_Beninden_Kanser_Api.Domain.Entities.User;
using Cilt_Beninden_Kanser_Api.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cilt_Beninden_Kanser_Api.WebAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly JwtTokenService _tokenService;

    public AuthController(IUserRepository userRepository, JwtTokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("E-posta ve şifre zorunludur.");

        var existing = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            return Conflict("Bu e-posta zaten kayıtlı.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = DomainUser.Create(request.Email, passwordHash, request.FullName);
        await _userRepository.AddAsync(user, ct);

        return Ok(new RegisterResponse(user.Id, user.Email, user.FullName));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("E-posta ve şifre zorunludur.");

        var user = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive)
            return Unauthorized("E-posta veya şifre hatalı.");

        var verified = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!verified)
            return Unauthorized("E-posta veya şifre hatalı.");

        var token = _tokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Id, user.Email, user.FullName));
    }

    public record RegisterRequest(string Email, string Password, string? FullName);
    public record RegisterResponse(Guid Id, string Email, string? FullName);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string Token, Guid UserId, string Email, string? FullName);
}
