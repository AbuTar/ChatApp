# CLI ChatAPP

## Server Code Breakdown (Documentation)
==========================================

### What is the purpose of this program?
------------------------------------------------------------------------
This is a CLI implementaiton of a basic TCP chat server
The server is reponsible for:
- Listening for client connections using a specific port (I have used 2025)
- Accept multiple clients using a seperate thread for each one
- Read a message from a client
- Echo the message back to the client
