using System.ComponentModel.DataAnnotations;

namespace TmsApi.Dtos;

public class StudentDto
{
    public int Id { get; set; }

    [MaxLength(20)]
    public required string RegistrationNumber { get; set; }

    [MaxLength(100)]
    public required string Name { get; set; }

    public decimal GPA { get; set; }
    public bool IsActive { get; set; }

    public uint Version { get; set; }
}