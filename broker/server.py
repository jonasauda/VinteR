import sys
import socket
import time

SERVER_ADDRESS = ("192.168.1.3", 43720)

connectionEstablished = False
connections = []
clientInfo = []


def decode_client_message(data):
    address_string = data.decode("utf-8")
    segments = address_string.split(":")
    return (segments[0], int(segments[1]))


def verify_client_message(message):
    try:
        ip, port = decode_client_message(message)
        return (ip, port)
    except:
        print("Failed to parse %s" % message)
        return None


def receive_client_message(conn):
    clientMessage = conn.recv(1024)

    # verify message
    result = verify_client_message(clientMessage)
    if result == None:
        raise Exception("Message from", addr, "is not valid")
    ip, port = result
    print("received address info '%s:%s' from %s" % (ip, port, addr))
    return clientMessage


def init_server_socket():
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.bind(SERVER_ADDRESS)
    return sock


with init_server_socket() as server_sock:
    while True:
        try:
            # accept connections and read addresses from messages
            while len(connections) < 2:
                try:
                    server_sock.listen(5)
                    conn, addr = server_sock.accept()
                    clientInfo += [receive_client_message(conn)]
                    connections += [[conn, addr]]
                except Exception as e:
                    print(e)

            # exchange addresses
            conn, addr = connections[0]
            conn.send(clientInfo[1])

            conn, addr = connections[1]
            conn.send(clientInfo[0])

            print("address exchange successful")
        except:
            print("Exception")
            connections.clear()
            clientInfo.clear()
            continue
        else:
            print("Successfull Exchange")
            connections.clear()
            clientInfo.clear()
    
