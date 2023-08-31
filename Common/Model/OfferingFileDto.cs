using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Common.Model
{
   public class OfferingFileDto
   {
      [JsonIgnore]
      public string OfferingFileIdentificator => FileName + ResourceInformer.offeringFilesJoint + FileSize;
      [JsonIgnore]
      public string EndpointsAndGradesJson { get; set; }

      public string FileName { get; set; } = string.Empty;
      public long FileSize { get; set; }

      /// <summary>
      /// Key: ipaddress:port
      /// Value: grade
      /// </summary>
      public Dictionary<string, int> EndpointsAndGrades { get; set; } = new Dictionary<string, int>();

      public OfferingFileDto()
      {

      }

      public OfferingFileDto(string EndPoint)
      {
         EndpointsAndGrades.Add(EndPoint, 0);
      }

      public string GetJson() => JsonConvert.SerializeObject(this, Formatting.Indented);
      public static OfferingFileDto? ToObjectFromJson(string jsonString) => JsonConvert.DeserializeObject<OfferingFileDto>(jsonString);

      public void MergeWithAnotherOfferingFileDto(OfferingFileDto offeringFileDto)
      {
         foreach (KeyValuePair<string, int> keyValuePair in offeringFileDto.EndpointsAndGrades)
         {
            this.EndpointsAndGrades[keyValuePair.Key] = 0;            
         }

         //this.EndpointsAndGrades.Keys.Intersect(offeringFileDto.EndpointsAndGrades.Keys).ToList().ForEach(key => this.EndpointsAndGrades[key] = 0);
      }
   }
}
