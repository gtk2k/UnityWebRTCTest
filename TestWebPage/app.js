const ws = new WebSocket('ws://localhost:8989');
ws.onopen = evt => {
    console.log('ws open');
};
ws.onmessage = async evt => {
    console.log(`${evt.data}`);
    const msg = JSON.parse(evt.data);
    if (msg.sdp) {
        await pc.setRemoteDescription(msg);
        if (msg.type === 'offer') {
            const answer = await pc.createAnswer();
            await pc.setLocalDescription(answer);
            ws.send(JSON.stringify(answer));
        } else if (msg.type === 'candidate') {
            await pc.addIceCandidate(msg);
        }
    }
};
ws.onclose = evt => {
    console.log(`ws close:${evt.code}`);
};
ws.onerror = _ => {
    console.log('ws error');
};

const pc = new RTCPeerConnection({ iceServers: [{ urls: 'stun:stun.l.google.com:19302' }] });
pc.onicecandidate = evt => {
    console.log(`onicecandidate: ${evt.candidate}`);
    if (evt.candidate)
        ws.send(JSON.stringify({ type: 'candidate', candidate: evt.candidate.candidate, sdpMid: evt.candidate.sdpMid, sdpMLineIndex: evt.candidate.sdpMLineIndex }));
};
pc.ontrack = evt => {
    console.log(`ontrack: ${evt.track.kind}`);
    if (evt.track.kind === 'video')
        vid.srcObject = evt.streams[0];
};
pc.ondatachannel = evt => {
    console.log(`data channel open:${evt.channel.name}`);
};
