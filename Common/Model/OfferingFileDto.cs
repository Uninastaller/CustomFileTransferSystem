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
        public string EndpointsAndGradesJson { get; set; } = string.Empty;

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

        public string GetJson() => JsonSerializer.Serialize(this);
        public static OfferingFileDto? ToObjectFromJson(string jsonString) => JsonSerializer.Deserialize<OfferingFileDto>(jsonString);

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
