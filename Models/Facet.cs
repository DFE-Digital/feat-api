using Azure.Search.Documents.Models;

namespace feat.api.Models;

public class Facet
{
    public Facet()
    {
        Values = new Dictionary<string, long>();
        Name = string.Empty;
    }
    
    public Facet(string name, IList<FacetResult> results)
    {
        Name = name;
        foreach (var facetResult in results)
        {
            var values = facetResult.AsValueFacetResult<string>();
            if (values == null) continue;
            Values.Add(values.Value, values.Count);
        }
    }
    
    public string Name { get; set; } = string.Empty;

    public Dictionary<string, long> Values { get; set; } = new();

}