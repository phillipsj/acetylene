using System.Text.Json;
public class Ignitor {

    public Ignitor() {

    }

    public IgnitionFile Parse(string contents) {
        var options = new JsonSerializerOptions() {
            PropertyNameCaseInsensitive = true
        };  
        return JsonSerializer.Deserialize<IgnitionFile>(contents,options);
    }
}
