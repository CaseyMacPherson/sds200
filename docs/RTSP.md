Summary<br>
RTSP version: RTSP/1.0<br>
Streaming Media: Audio only (no video)<br>
Media Format: G.711 u-law <br>
Media Filename: scanner.au <br>
Media Server Name: Scanner Audio Server 0.0.1 <br>
Cache-Control: no-cache <br>
Transport: RTP/AVP unicast <br>
Supported RTSP Method: <br>
&emsp;PLAY <br>
&emsp;TEARDOWN <br>
&emsp;GET_PARAMETER <br>
&emsp;SETUP <br>
&emsp;DESCRIBE <br>
&emsp;OPTIONS  <br>


Scanner received on TCP and responds via UDP

```mermaid
sequenceDiagram
    participant A as App
    participant S as Scanner

    A->>S: OPTIONS rtsp://000.000.000.000/au:scanner.au RTSP/1.0
    NOTE over A,S: dst port=554(rtsp)
    S->>A: RTSP/1.0 200 OK
    A->>S: DESCRIBE rtsp://000.000.000.000/au:scanner.au RTSP/1.0
    S->>A: RTSP/1.0 200 OK, with session description
    A->>S: SETUP rtsp://000.000.000.000/au:scanner.au/trackID=1 RTSP/1.0
    S->>A: RTSP/1.0 200 OK
    A->>S: PLAY rtsp://000.000.000.000/au:scanner.au/ RTSP/1.0
    S->>A: RTSP/1.0 200 OK
    A->>S: GET_PARAMETER rtsp://000.000.000.000/au:scanner.au/ RTSP/1.0
    S->>A: RTSP/1.0 200 OK
    S->>A: RTP(udp) streaming
    A->>S: TEARDOWN rtsp://000.000.000.000/au:scanner.au/ RTSP/1.0
```

RTSP METHOD
(*A)Global IP Address of Scanner
(*B)Depends on app's device
(*C)Scanner Assigned number
(*D)app Assigned number(port number)

<table>
  <thead>
    <tr>
      <th>Direction</th>
      <th>Method</th>
      <th>Contents</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>A--&gt;S</td>
      <td>OPTIONS</td>
      <td>To Get Supported RTSP Method<br>OPTIONS rtsp://xxx.xxx.xxx.xxx (*A)/au:scanner.au RTSP/1.0<br>CSeq: 2<br>User-Agent: xxxxxxxxxxx(*B)</td>
    </tr>
    <tr>
      <td>S--&gt;A</td>
      <td>Reply</td>
      <td>DESCRIBE, SETUP, TEARDOWN, PLAY, OPTIONS, GET_PARAMETER methods supported.<br>RTSP/1.0 200 OK<br>Supported: play.basic, con.persistent<br>Cseq: 2<br>Server: Scanner Audio Server 0.0.1<br>Public: DESCRIBE, SETUP, TEARDOWN, PLAY, OPTIONS, GET_PARAMETER<br>Cache-Control: no-cache</td>
    </tr>
    <tr>
      <td>A--&gt;S</td>
      <td>DESCRIBE</td>
      <td>To Get Session Description(Media Format…)<br>DESCRIBE rtsp://xxx.xxx.xxx.xxx (*A)/au:scanner.au RTSP/1.0<br>CSeq: 3<br>User-Agent: xxxxxxxxxxx(*B)<br>Accept: application/sdp</td>
    </tr>
    <tr>
      <td rowspan="2">S--&gt;A</td>
      <td>REPLY</td>
      <td>/w sdp<br>RTSP/1.0 200 OK<br>Content-Base: rtsp://xxx.xxx.xxx.xxx (*A)/au:scanner.au/<br>Date: Tue, 3 Dec 2013 08:10:30 UTC<br>Content-Length: 575<br>Session: 593922212;timeout=60<br>Expires: Tue, 3 Dec 2013 08:10:30 UTC<br>Cseq: 3<br>Content-Type: application/sdp<br>Server: Scanner Audio Server 0.0.1<br>Cache-Control: no-cache</td>
    </tr>
    <tr>
      <td>sdp</td>
      <td>v=0<br>o=- 0000000000(*c) IN IP4 127.0.0.1<br>s=scanner.au<br>c=IN IP4 0.0.0.0<br>t=0 0<br>a=sdplang:en<br>a=control:<br>m=audio 0 RTP/AVP 0<br>a=control:trackID=1</td>
    </tr>
    <tr>
        <td>A--&gt;S</td>
        <td>SETUP</td>
        <td>To decide UDP portnumber<br>SETUP rtsp://xxx.xxx.xxx.xxx (*A)/au:scanner.au/trackID=1 RTSP/1.0<br>CSeq: 4<br>User-Agent: xxxxxxxxxxx(*B)<br>Transport: RTP/AVP;unicast;client_port=0000(*D)</td>
    </tr>
    <tr>
        <td>S--&gt;A</td>
        <td>Reply</td>
        <td>UDP port number for RTP streaming<br>RTSP/1.0 200 OK<br>Date: Tue, 3 Dec 2013 08:10:30 UTC<br>Transport: RTP/AVP;unicast;client_port=0000(*D);source=xxx.xxx.xxx.xxx (*A);server_port=0000(*C);ssrc=00000000(*C)<br>Session: 0000000000(*c);timeout=60<br>Expires: Tue, 3 Dec 2013 08:10:30 UTC<br>Cseq: 4<br>Server:Scanner Audio Server 0.0.1<br>Cache-Control: no-cache</td>
    </tr>
    <tr>
        <td>A--&gt;S</td>
        <td>PLAY</td>
        <td>To Open RTP(UDP)<br>PLAY rtsp://xxx.xxx.xxx.xxx (*A)/au:scanner.au/ RTSP/1.0<br>CSeq: 6<br>User-Agent: xxxxxxxxxxx(*B)<br>Session: 0000000000(*C)<br>Range: npt=0.000-</td>
</tr>
<tr> 
    <td>S--&gt;A</td>
    <td>Reply</td>
    <td>RTP(UDP) streaming<br>RTSP/1.0 200 OK<br>Range: npt=0.0-596.48<br>Session: 0000000000(*c);timeout=60<br>Cseq: 6<br>RTP-Info: url=rtsp://xxx.xxx.xxx.xxx (*A)/au:scanner.au/trackID=1;seq=1;rtptime=0<br>Server:Scanner Audio Server 0.0.1<br>Cache-Control: no-cache</td>
</tr>
<tr>
    <td>A--&gt;S</td>
    <td>GET_PARAMETER</td>
    <td>To Keep RTSP(RTP) alive<br>GET_PARAMETER rtsp://xxx.xxx.xxx.xxx (*A)/au:scanner.au/ RTSP/1.0<br>CSeq: 5<br>User-Agent: xxxxxxxxxxx(*B)<br>Session: 0000000000(*C)</td>
</tr>
<tr>
    <td>S--&gt;A</td>
    <td>Reply</td>
    <td>RTSP/1.0 200 OK<br>Session: 0000000000(*c);timeout=60<br>Cseq: 5<br>Server:Scanner Audio Server 0.0.1<br>Cache-Control: no-cache</td>
</tr>
<tr>
    <td>A--&gt;S</td>
    <td>TEARDOWN</td>
    <td>To Stop RTSP(RTP)<br>TEARDOWN rtsp://xxx.xxx.xxx.xxx (*A)/au:scanner.au/ RTSP/1.0<br>CSeq: 8<br>User-Agent: xxxxxxxxxxx(*B)<br>Session: 0000000000(*C)</td>
</tr>
</tbody>
</table>

A: App
S: Scanner
