using System;
using System.Threading.Tasks;
using Microsoft.Agents.AI;

public class Test {
    public static async Task Run(AIAgent agent) {
        await foreach (var chunk in agent.RunStreamingAsync("test")) {
            Console.WriteLine(chunk.GetType().AssemblyQualifiedName);
            var props = chunk.GetType().GetProperties();
            foreach (var prop in props) {
                Console.WriteLine(" - " + prop.Name + " (" + prop.PropertyType.Name + ")");
            }
            break;
        }
    }
}
