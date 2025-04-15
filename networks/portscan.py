import socket

print("enter ip adress:")

ip = input()
porst  = [80,3400,3005]

for port in porst:
    sock =socket.socket(socket.AF_INET,socket.SOCK_STREAM)
    sock.settimeout(1)
    result = sock.connect_ex((ip,port))
    if result == 0:
        print(f"port {port} is open")
    else:
         print(f"port {port} is close")
    sock.close()
    
