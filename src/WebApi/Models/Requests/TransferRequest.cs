using System.ComponentModel.DataAnnotations;

namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Requests;

public sealed record TransferRequest([Required, MaxLength(64)] string TargetAccountNumber, decimal Amount,
    [Required, MaxLength(128)] string Pin);
