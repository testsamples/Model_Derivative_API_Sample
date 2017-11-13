/* 
 * Forge SDK
 *
 * The Forge Platform contains an expanding collection of web service components that can be used with Autodesk cloud-based products or your own technologies. Take advantage of Autodesk’s expertise in design and engineering.
 *
 * OpenAPI spec version: 0.1.0
 * Contact: forge.help@autodesk.com
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Autodesk.Forge.Model
{
    /// <summary>
    /// list of the requested outputs
    /// </summary>
    [DataContract]
    public partial class JobAcceptedJobs :  IEquatable<JobAcceptedJobs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobAcceptedJobs" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected JobAcceptedJobs() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="JobAcceptedJobs" /> class.
        /// </summary>
        /// <param name="Output">identical to the request body. For more information please see the request body structure above. (required).</param>
        public JobAcceptedJobs(Object Output = null)
        {
            // to ensure "Output" is required (not null)
            if (Output == null)
            {
                throw new InvalidDataException("Output is a required property for JobAcceptedJobs and cannot be null");
            }
            else
            {
                this.Output = Output;
            }
        }
        
        /// <summary>
        /// identical to the request body. For more information please see the request body structure above.
        /// </summary>
        /// <value>identical to the request body. For more information please see the request body structure above.</value>
        [DataMember(Name="output", EmitDefaultValue=false)]
        public Object Output { get; set; }
        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class JobAcceptedJobs {\n");
            sb.Append("  Output: ").Append(Output).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            // credit: http://stackoverflow.com/a/10454552/677735
            return this.Equals(obj as JobAcceptedJobs);
        }

        /// <summary>
        /// Returns true if JobAcceptedJobs instances are equal
        /// </summary>
        /// <param name="other">Instance of JobAcceptedJobs to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(JobAcceptedJobs other)
        {
            // credit: http://stackoverflow.com/a/10454552/677735
            if (other == null)
                return false;

            return 
                (
                    this.Output == other.Output ||
                    this.Output != null &&
                    this.Output.Equals(other.Output)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            // credit: http://stackoverflow.com/a/263416/677735
            unchecked // Overflow is fine, just wrap
            {
                int hash = 41;
                // Suitable nullity checks etc, of course :)
                if (this.Output != null)
                    hash = hash * 59 + this.Output.GetHashCode();
                return hash;
            }
        }
    }

}

