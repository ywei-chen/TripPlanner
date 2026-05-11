using TripPlanner.Application.Common.DTOs.Auth;

namespace TripPlanner.Application.Auth.Commands;

public record RegisterCommand(string Email, string Username, string Password);
