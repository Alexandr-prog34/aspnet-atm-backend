namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Requests;

using System.ComponentModel.DataAnnotations;

public record WithdrawRequest(decimal Amount, [Required, MaxLength(128)] string Pin);
