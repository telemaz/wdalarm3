# WatchDog Alarm
The goal of this piece of software is to facilitate triggering action when certain condition are not met. The premier reason would be to implement a mecanism where, if no action is taken to prevent an alarm state, an action is triggered after a set delay. 
Here are some target example.\
\
This could be useful for people living alone, so they can indicate they are still alive.\
For an action to occur if someone die like sending a password list to a family member.\
To keep track of an expedition in a danger zone.

## Functionnality
- An action can be triggered when an alarm goes active.
- An alarm timer is normaly reset when it is "pinged".
- An action can be triggered when an alarm is "pinged".
- The ping may contain a payload.
- To be effective a ping may need to respect some condition, like a challenge response verification.
- An historic of all pings, and alarms triggered with theres actions should be kept for a certain amount of time for consultation.
- The action should be at least send an email, send an SMS, calling a REST API. 
- The alarms should be trigger within 5 sec of the target time.
- The target should be at minimum 15sec after being set.
- An alarm delay should be set with many different possibility such as: a timeout, a schedule, a timeout determined by the ping payload.
- Multiple delay could be set for an alarm. All these delays triggering a different action.
- A ping should not necessarly postpone the alarm,
- The system should be infinitly scalable

## Action type

#### Rest API call
Payload max size : 4096 bytes\
Parameters:
```
{ "url":"https://example.com/API",
  "method":"GET"/"POST"/"PUT"/"DELETE",
  "user":"username",
  "password":"passw0rd",
  "payload":"" }
```


#### Email
Payload max size : 4096 bytes\
Parameters:
```
{  "mailto":"username@example.com",
   "smtp":"https://example.com/API",
   "encryption":"STARTTLS"/"TLS",
   "user":"username",
   "password":"passw0rd",
   "to":"",
   "cc":"",
   "cci":"",
   "subject":"XXX trigerred",
   "payload":"" } 
```


#### SMS
Payload max size : 160 bytes\
Parameters:
```
{ "number":"+14445552357",
   "payload":"" } 
```

## Backend

The service if adopted by a larger quantity of user should be properly scalable. It should not cost to much money to run properly to prevent an early doom.
As such the best option would probably be to use a serverless architecture. At least in the first period of production.


The data could be stored in a Timeseries databases such as AWS Timestream coupled with AWS Lambda for the pings and actions.

## Security
There is a major security concerns for the data stored in database. Idealy, a zero-knowledge would be the best option. Unfortunatly, a great part of the use case require for a unknowing third party to receive the data. For it to be useful, the third party should be able to derypt those data without any information. 
