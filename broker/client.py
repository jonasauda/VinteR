import socket
import sys
import time
import urllib.request

# constants
ID = 0
if len(sys.argv) > 1:
    ID = int(sys.argv[1])

# SERVER_ADDRESS = ("helin.uberspace.de", 43720)
SERVER_ADDRESS = ("46.101.139.210", 43720)
PEER_MESSAGE = bytes("Hello from client %s" % ID, "UTF-8")

# helper functions
def encode_server_message(address):
    return (address[0] + ":" + str(address[1])).encode("utf-8")


def decode_server_message(data):
    address_string = data.decode("utf-8")
    segments = address_string.split(":")
    return (segments[0], int(segments[1]))


# determine external IP
print("determining external IP...")
external_ip = urllib.request.urlopen("https://api.ipify.org").read().decode("utf8")
# external_ip = urllib.request.urlopen("https://ident.me").read().decode("utf8")
print("external IP:", external_ip)

# variables with dependencies
CLIENT_ADDRESS = (external_ip, 27001 + ID)
REGISTER_MESSAGE = encode_server_message(CLIENT_ADDRESS)

# server communication
def init_tcp_socket():
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.bind(("0.0.0.0", CLIENT_ADDRESS[1]))
    sock.connect(SERVER_ADDRESS)
    return sock


def send_server_message(sock):
    sock.sendall(REGISTER_MESSAGE)
    print("sent", REGISTER_MESSAGE, "to", SERVER_ADDRESS)


def receive_server_message(sock):
    data = sock.recv(1024)
    print("received", data, "from server")
    try:
        addr = decode_server_message(data)
        print("received peer address:", addr)
        return addr
    except:
        print("Failed to parse message: %s" % data)


# peer communication
def init_udp_socket():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind(("0.0.0.0", CLIENT_ADDRESS[1]))
    sock.settimeout(2.0)
    return sock


def send_peer_message(sock, addr):
    sock.sendto(PEER_MESSAGE, addr)
    print("sent", PEER_MESSAGE, "to", addr)


def receive_peer_message(sock):
    try:
        while True:
            data, addr = sock.recvfrom(1024)
            print("received", data, "from", addr)
    except socket.timeout:
        print("")


# main logic
peer_address = None

# exchange address
with init_tcp_socket() as tcp_socket:
    send_server_message(tcp_socket)
    while peer_address is None:
        peer_address = receive_server_message(tcp_socket)
    tcp_socket.close()

# ping peer
with init_udp_socket() as udp_socket:
    print("sending to peer...")
    while True:
        send_peer_message(udp_socket, peer_address)
        receive_peer_message(udp_socket)
        time.sleep(2)

print("done")
