# REST to SOAP Gateway with WS-Security

This application acts as a bridge between REST and SOAP, converting RESTful requests into SOAP messages while implementing WS-Security standards. It securely transmits the transformed requests to an .svc endpoint, ensuring authentication, encryption, and integrity.

> **NOTE:** This application is built around an existing system called SecureX and Owned by Comcorp visit [comcorp.co.za](http://comcorp.co.za/) for more information

## ✨ Features

- Converts REST API requests into SOAP messages
- Implements WS-Security for message signing and encryption
- Supports AES-256-CBC for encryption and RSA 1.5 for key transport
- Uses X.509 certificates for secure authentication
- Sends secured SOAP requests to an .svc endpoint
- Ensures compliance with industry security standards

## 🔧 Technologies Used

- .NET Core 8
- WCF (Windows Communication Foundation)
- SOAP & WS-Security
- X.509 Certificates
- AES-256 & RSA Encryption
