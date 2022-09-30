# NamedPipes - Interprocess Comunication

Una *pipe* è una sezione della memoria condivisa che viene utilizzata per la comunicazione. Il processo che crea una pipe è il *server pipe* . Un processo che si connette a una pipe è un *client pipe* . Un processo scrive le informazioni nella pipe, quindi l'altro processo legge le informazioni dalla pipe.

Esistono due tipi di pipe: [pipe anonime](https://docs.microsoft.com/en-us/windows/win32/ipc/anonymous-pipes) e pipe con [nome](https://docs.microsoft.com/en-us/windows/win32/ipc/named-pipes) . Le pipe anonime richiedono un sovraccarico inferiore rispetto alle pipe con nome, ma offrono servizi limitati, entrambe vengono utilizzate come condotto di informazioni. 

Una *named pipe* è una pipe denominata, unidirezionale o duplex per la comunicazione tra il server pipe e uno o più client pipe. Tutte le istanze di una named pipe condividono lo stesso nome di pipe

Qualsiasi processo può accedere alle named pipe e può fungere sia da server che da client, rendendo possibile la comunicazione peer-to-peer

Le pipe con nome possono essere utilizzate per fornire la comunicazione tra processi sullo stesso computer o tra processi su computer diversi in una rete.

![Untitled](NamedPipes%20-%20Interprocess%20Comunication%2073b122c9a7e14dbbb679c84c46a3df44/Untitled.png)

Il serverPipe tiene attivo un thread per ogni client, occorre quindi definire il numero massimo di connessioni possibili.

![Untitled](NamedPipes%20-%20Interprocess%20Comunication%2073b122c9a7e14dbbb679c84c46a3df44/Untitled%201.png)

![Untitled](NamedPipes%20-%20Interprocess%20Comunication%2073b122c9a7e14dbbb679c84c46a3df44/Untitled%202.png)

Esempio di funzionamento : 

Il pipeServer accetta come attributi nel costruttore il nome della pipe ed il numero massimo di connessioni instanziabili per i client.

Il pipeMessage e’ una classe custom che tramite serializzazione viene trasmessa nella pipe

```csharp
public class PipeMessage
    {
        public string topic { get; set; }
        public string Message { get; set; }

        //constructor
        public PipeMessage()
        {

        }
        public PipeMessage(string topic, string message)
        {
            this.topic = topic;
            this.Message = message;
        }
        public byte[] Serialize()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(topic);
                    writer.Write(Message);
                }
                return m.ToArray();
            }
        }

        public static PipeMessage Deserialize(byte[] data)
        {
            PipeMessage result = new PipeMessage();

            using (MemoryStream m = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    result.topic = reader.ReadString();
                    result.Message = reader.ReadString();
                }
            }
            return result;
        }

        //tostring
        public override string ToString()
        {
            return string.Format("[topic: {0} , Message: {1}]", topic, Message);
        }
    }
```

Esempio di comunicazione client - server

```csharp
IPipeServer _server = new PipeServer("flaminio", 10);
IPipeClient _client = new PipeClient(_server.ServerId);

_server.Start();
IPipeClient _client = new PipeClient(_server.ServerId);
//IPipeClient _client = new PipeClient("nome della pipe);

PipeMessage message= new PipeMessage("flaminio", "client message");

//sottoscrizione ad evento di ricezione messaggi
_server.MessageReceivedEvent += (sender, argss) =>
  {
      message = argss.Message;
  };

//sottoscrizione ad evento di disconnessione di un client
_server.ClientDisconnectedEvent += (sender, argss) =>
  {
    _logger.Info("Client disconnected " + argss.ClientId);
  };

 //evento di connessione
 _server.ClientConnectedEvent += (sender, argss) =>
  {
      _logger.Info("Client connected " + argss.ClientId);
  };

_client.SendMessage(pipe);
_server.SendMessage(pipe);
```

In questo esempio possiamo vedere una comunicazione full-duplex in cui client e server riescono a inviare e ricevere messaggi lungo la stessa pipe.

Le classi `IPipeServer` e `IPipeClient`  espongono tutti i metodi disponibili. 

Esempio di comunicazione tramite pipe in `c++`

```cpp
#include <windows.h>
#include <iostream>

HANDLE fileHandle;
using namespace std;

void ReadString(char* output) {
    ULONG read = 0;
    int index = 0;
    do {
        ReadFile(fileHandle, output + index++, 1, &read, NULL);
    } while (read > 0 && *(output + index - 1) != 0);
}

int main()
{
    // create file
    fileHandle = CreateFileW(TEXT("\\\\.\\pipe\\flaminio"), GENERIC_READ | GENERIC_WRITE, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
    wcout << "C++ send data to pipe..." << endl;

      // send data to server
    const char* msg = "hello from c++\r\n";
    WriteFile(fileHandle, msg, strlen(msg), nullptr, NULL);
    wcout << "Done to send  " << endl;
    system("pause");

}
```