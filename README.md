# AuthNet
> Netduino powered two-step authentication for your home

## Installation

There are two projects: Netduino and Node.
Setup the Node server first, followed by the Netduino.

### Node

To install all of the Node.js dependencies run:

```bash
npm install
```

Then setup your Twilio account with:

```bash
export TWILIO_ACCOUNT_SID=<your account sid>
export TWILIO_AUTH_TOKEN=<your auth token>
```

Then you can start the Node server:

```bash
npm start
```

### Netduino

Open the project solution in Visual Studio 2013 that is setup for Netduino development.
See *development environment* section of http://www.netduino.com/downloads/

Change the `_Host` to be the IP address of your Node server.

Go to menu `Debug` and click `Start Debugging`.
Click `Continue` for any exceptions that occur.



