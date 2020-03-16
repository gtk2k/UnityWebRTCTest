const ws = new WebSocket('ws://localhost:8989');
ws.onopen = evt => {
    console.log('ws open');
};
ws.onmessage = async evt => {
    const msg = JSON.parse(evt.data);
    console.log(msg.type);
    if (msg.sdp) {
        console.log(`set remote offer: ${evt.data}`);
        await pc.setRemoteDescription(msg);
        if (msg.type === 'offer') {
            const answer = await pc.createAnswer();
            await pc.setLocalDescription(answer);
            const sendData = JSON.stringify(answer);
            console.log(`send answer: ${sendData}`);
            ws.send(sendData);
        }
    } else if (msg.type === 'candidate') {
        try {
            console.log(`add ice candidate: ${evt.data}`);
            await pc.addIceCandidate(msg);
        } catch (ex) {
            console.log(ex.message);
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
    if (evt.candidate) {
        const sendData = JSON.stringify({ type: 'candidate', candidate: evt.candidate.candidate, sdpMid: evt.candidate.sdpMid, sdpMLineIndex: evt.candidate.sdpMLineIndex });
        console.log(`send candidate: ${sendData}`);
        ws.send(sendData);
    }
};
pc.ontrack = evt => {
    console.log(`ontrack: ${evt.track.kind}`);
    if (evt.track.kind === 'video')
        vid.srcObject = evt.streams[0];
};
pc.ondatachannel = evt => {
    console.log(`data channel open:${evt.channel.name}`);
};
