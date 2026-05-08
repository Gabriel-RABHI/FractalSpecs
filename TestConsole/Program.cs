using System;
using System.Reflection;
using Microsoft.Agents.AI;
using OpenAI.Chat;

public class Program {
    public static void Main() {
        var asm = Assembly.GetAssembly(typeof(StreamingChatCompletionUpdate));
        Console.WriteLine("Version: " + asm.GetName().Version);
    }
}
