using System.ComponentModel.DataAnnotations;

namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Requests;

public sealed record LinkOwnerRequest([Required, MaxLength(200)] string OwnerSubject);
