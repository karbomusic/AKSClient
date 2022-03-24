# TCP/HTTP Test Client

This is as-is personal use at your own risk test code - you've been warned!

This app functions as a web client that an send multiple asynchronus requests to a remote enpoint.
It uses a nested loop to send blocks of spewed requests.
Usage: `AKSClientTest.exe  <URL> <NumRequests> <DelayBetweenBlocks> <DelayBetweenRequests>` 
 
URL: The remote endpoint. If using the sister server app ?delay= will tell the endpoint to hold
the request for a random number of seconds between 0 and the delay value.

**NumRequests:** Number of requests * number of requests. So 10 = 10 blocks of 10 or 100 requests total.

**DelayBetweenRequests:** The micro delay between every request.

**DelayBetweenBlocks:** This is how long to pause per block of requests so if NumRequests = 10 and DelayBetwenBLocks = 1
                    then there will be a 1 second pause every 10 requests.

**Usage Examples:**
  
  `.\AKSRequestClient.exe http://SomeRemoteEndpoint:5000/main?delay=3 100 1 50`
  
  Make 10k requests (100*100) wait 1 second between each block of 100, each individual request inside a block is 50ms. 
  Randomize the server hold time between 0 and 3    seconds.
  
  `.\AKSRequestClient.exe http://SomeRemoteEndpoint:5000/main?delay=10 100 1 100`
  
  Make 10k requests (100*100) wait 1 second between each block of 100, each individual request inside a block is 100ms. 
  Randomize the server hold time between 0 and 10  seconds.
  
  `.\AKSRequestClient.exe http://SomeRemoteEndpoint:5000/main?delay=15 1000 2 25`

  Make 1,000,000 requests (1000*1000) wait 2 seconds between each block of 1000, each individual request inside a block is 25ms apart. 
  Randomize the server hold time between 0 and 15 seconds.
