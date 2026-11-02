using System.ComponentModel.DataAnnotations;

namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Requests;

public record CreateAccountRequest(
    [Required, MaxLength(64)] string AccountNumber,
    [Required, MaxLength(128)] string Pin,
    decimal InitialBalance,
    [Required, MaxLength(200)] string OwnerSubject);
