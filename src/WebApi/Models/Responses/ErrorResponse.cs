namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Responses;

public record ErrorResponse(string Error, string? Details = null);