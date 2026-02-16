namespace JobBoard.AI.Application.Actions.Drafts.DraftsByCompany;

public class DraftsByCompanyResponse
{
      public Dictionary<Guid, DraftsByCompanyItemResponse> DraftsByCompany { get; set; } = new();
}

public class DraftsByCompanyItemResponse
{
     public List<DraftResponse> Drafts { get; set; } = new();    
     public int Count => Drafts.Count;
}
