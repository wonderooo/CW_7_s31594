namespace VetSqlClient.Models.DTOs;

public class TripGetDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string DateFrom { get; set; }
    public string DateTo { get; set; }
    public int MaxPeople { get; set; }
    
    public List<CountryDTO> Countries { get; set; }
}