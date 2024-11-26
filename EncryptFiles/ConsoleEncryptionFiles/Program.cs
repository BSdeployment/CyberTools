// See https://aka.ms/new-console-template for more information

Console.WriteLine("you are wand to encrypte all file in thie folder note you need secure password ");




Console.WriteLine("insert y/n");

var result = Console.ReadLine();

if(result =="y")
{
    Console.WriteLine("to get started insert your key");
    var key = Console.ReadLine();
    if(key == null)
    {
        Console.WriteLine("no key exist");
    }
    else
    {
        Console.WriteLine("we ready encrypt. please make sure you remember your key!");
        Console.WriteLine("get started? (y/n)");

        string? start = Console.ReadLine();

        if(start is not null)
        {
            if(start == "y")
            {
                string password = key;
                string currentDirectory = Directory.GetCurrentDirectory();

                Console.WriteLine("1. Encrypt all files");
                Console.WriteLine("2. Decrypt all files");
                Console.Write("Choose an option (1/2): ");
                string? choice = Console.ReadLine();

                try
                {
                    if (choice == "1")
                    {
                       EncryptServices.EncryptAllFilesInDirectory(currentDirectory, password);
                        Console.WriteLine("All files encrypted successfully.");
                    }
                    else if (choice == "2")
                    {
                        EncryptServices.DecryptAllFilesInDirectory(currentDirectory, password);
                        Console.WriteLine("All files decrypted successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Invalid choice.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("good bye!");
            }
        }
        else
        {
            Console.WriteLine("good bye!");
        }
    }

}
else
{
    Console.WriteLine("good day for you!");
}



