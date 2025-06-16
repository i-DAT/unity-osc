# Unity OSC Server

A Unity package for receiving OSC messages.

## Installation

Follow the [package install guide](https://docs.unity3d.com/6000.1/Documentation/Manual/upm-ui-giturl.html), using this repository URL.

Alternatively, download the contents [`Runtime/`](Runtime/) and place the scripts into your project `Assets` folder.

## Usage

Add the `OSCManager` script to an object in the scene (eg an empty called `OSC Manager`) and set the port number in the inspector.

Register handlers on the object which will be called when a given OSC message type is received.
The `defaultHandler` field can also be set to a fallback handler method.

See [`Examples/RotateObject.cs`](Examples/RotateObject.cs) for how to register handlers.
