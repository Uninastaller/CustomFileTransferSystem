using Common.Enum;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Model
{
   public class OfferingFileDto
   {
      [JsonIgnore]
      public string OfferingFileIdentificator => FileName + "_" + FileSize;
      [JsonIgnore]
      public string EndpointsAndPropertiesJson { get; set; } = string.Empty;

      public string FileName { get; set; } = string.Empty;
      public long FileSize { get; set; }

      /// <summary>
      /// Key: ipaddress:port
      /// Value: EndpointProperties
      /// </summary>
      public Dictionary<string, EndpointProperties> EndpointsAndProperties { get; set; } = new Dictionary<string, EndpointProperties>();

      public OfferingFileDto()
      {

      }

      public OfferingFileDto(string endPoint, TypeOfServerSocket typeOfServerSocket)
      {
         EndpointsAndProperties.Add(endPoint, new EndpointProperties() { Grade = 0, TypeOfServerSocket = typeOfServerSocket });
      }

      public string GetJson() => JsonSerializer.Serialize(this);
      public static OfferingFileDto? ToObjectFromJson(string jsonString) => JsonSerializer.Deserialize<OfferingFileDto>(jsonString);

   }

   // Helper Class
   public class EndpointProperties
   {
      public int Grade { get; set; }
      public TypeOfServerSocket TypeOfServerSocket { get; set; }
   }

}
