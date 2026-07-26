namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Responses;

public record LoginResponse(Guid SessionKey, string Role);