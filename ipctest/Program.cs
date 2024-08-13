// See https://aka.ms/new-console-template for more information

using System.IO.MemoryMappedFiles;
using System.Text;

const string FILE_MAP_NAME = "Global\\DsHidMiniSharedMemory";
const string EVENT_NAME = "Global\\DsHidMiniWriteEvent";
const int BUFFER_SIZE = 4096;
var mutexName = "Global\\DsHidMiniMutex";


try
{
    // Attempt to open the existing named mutex
    using (var mutex = Mutex.OpenExisting(mutexName))
    {
        Console.WriteLine("Successfully opened the existing mutex.");

        // Wait for the mutex to ensure exclusive access
        mutex.WaitOne();

        Console.WriteLine("Mutex acquired. Performing some work...");

        // Open the named event
        var eventHandle = EventWaitHandle.OpenExisting(EVENT_NAME);
    
        Console.WriteLine("Waiting on event");

        // Wait for the event to be signaled
        eventHandle.WaitOne();

        Console.WriteLine("Event signaled");

        // Open the existing memory-mapped file
        using (var mmf = MemoryMappedFile.OpenExisting(FILE_MAP_NAME))
        {
            // Create a view accessor to read from the memory-mapped file
            using (var accessor = mmf.CreateViewAccessor(0, BUFFER_SIZE))
            {
                var buffer = new byte[BUFFER_SIZE];

                // Read from the memory-mapped file
                accessor.ReadArray(0, buffer, 0, BUFFER_SIZE);

                // Convert the byte array to a string
                var message = Encoding.Default.GetString(buffer).TrimEnd('\0');

                Console.WriteLine("Read from memory-mapped file: " + message);
            }
        }

        Console.WriteLine("Work done. Releasing mutex.");

        // Release the mutex
        mutex.ReleaseMutex();
    }
}
catch (WaitHandleCannotBeOpenedException)
{
    Console.WriteLine($"A mutex with the name '{mutexName}' does not exist.");
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("Access to the mutex is denied.");
}
catch (Exception ex)
{
    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
}


Console.WriteLine("Press any key to exit...");
Console.ReadKey();