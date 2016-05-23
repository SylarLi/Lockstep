//# 主

var Server = require('./Server.js');
var Packet = require('./Packet.js');
var LockStepEnv = require('./LockStepEnv.js');
var ClientFrameData = require('./ClientFrameData.js');
var RUShort = require('./RUShort.js');

var Host = '127.0.0.1';
var Port = 9001;

var TPF = 0.0333333;
var PDBL = 3;

var lsenv = new LockStepEnv();

var server = new Server();
server.bind(Host, Port);
server.onPacketReceived = onPacketReceived;
server.onPacketArrived = onPacketArrived;
server.onPacketLoss = onPacketLoss;

// 帧循环
var lstick = 0.0;
var lastTime = Date.now();
setInterval(function() {
	var currentTime = Date.now();
	var deltaTime = currentTime - lastTime;
	lastTime = currentTime;
	if (lsenv.start) {
		onLockStepStart();
		var acctime = lstick + deltaTime;
        var deltaFrame = parseInt(acctime / TPF);
        lstick = acctime - deltaFrame * TPF;
        if (deltaFrame > 0) {
        	lsenv.frame = lsenv.frame.add(new RUShort(deltaFrame));
        }
	}
	server.update(deltaTime);
}, TPF);

process.stdin.setEncoding('utf8');
process.stdin.on('readable', function() {
  var chunk = process.stdin.read();
  if (chunk !== null) {
    if (chunk.toString().replace(/\r*\n$/g, '') == 'start') {
    	Start();
    }
  }
});

function Start() {
	lsenv.start = true;
	server.dispatchAll(null, Packet.Type.Message);
}

// 发送[lsenv.from, lsenv.to]
// 帧 --> 玩家 --> 操作
function onLockStepStart () {
	if (lsenv.to.laterThan(lsenv.from) && lsenv.to.minus(lsenv.from).value + 1 >= PDBL) {
		var datas = [];
		var bufferlen = 0;
		var clients = server.clients;
		for (var i = lsenv.from; i.before(lsenv.to.add(new RUShort(1))); i = i.add(new RUShort(1))) {
			var frameData = [];
			for (var id in clients) {
				var client = clients[id];
				frameData[client.index] = client.frameDatas[i.value];
				bufferlen += frameData[client.index].data.length;
			}
			bufferlen += 2;		// 帧序号长度
			bufferlen += 1;		// 玩家操作列表长度
			datas.push(frameData);
		}
		bufferlen += 2;	// 帧操作列表长度
		var frame = lsenv.from;
		var index = 0;
		var buffer = new Buffer(bufferlen);
		buffer.writeUInt16LE(datas.length, index, true);
		index += 2;
		for (var i = 0; i < datas.length; i++) {
			buffer.writeUInt8(frame.value, index, true);
			index += 1;
			var frameData = datas[i];
			buffer.writeUInt8(frameData.length, index, true);
			index += 1;
			for (var j = 0; j < frameData.length; j++) {
				var frameDataBuffer = frameData[j].data;
				frameDataBuffer.copy(buffer, index, 0, frameDataBuffer.length);
				index += frameDataBuffer.length;
			}
			frame = frame.add(new RUShort(1));
		}
		server.dispatchAll(buffer, Packet.Type.LockStep);
		lsenv.from = lsenv.to.add(new RUShort(1));
	}
}

function onPacketReceived (client, packet) {
	if (packet.type == Packet.Type.LockStep) {
		var buffer = packet.data;
		var index = 0;
		var frame = new RUShort(buffer.readUInt8(index, true));
		index += 1;
		console.log("received : " + frame.value);
		if (!client.frameDatas[frame.value]) {
			var fdata = new ClientFrameData(frame);
			fdata.data = new Buffer(buffer.length - index);
			buffer.copy(fdata.data, 0, index, buffer.length);
			client.frameDatas[frame.value] = fdata;
			updateLsenv();
		}
		else {
			console.log('repeat frame data : ' + frame.value + ' from : ' + client.id);
		}
	}
}

function onPacketArrived (client, packet) {
	if (packet.type == Packet.Type.LockStep) {
		var buffer = packet.data;
		var framelen = buffer.readUInt16LE(0, true);
		var from = new RUShort(buffer.readUInt8(2, true));
		for (var i = 0; i < framelen; i++) {
			var frame = from.add(new RUShort(i));
			if (client.frameDatas[frame.value]) {
				delete client.frameDatas[frame.value];
			}
			console.log('arrived : ' + frame.value);
		}
	}
}

function onPacketLoss (client, packet) {
	if (packet.type == Packet.Type.Message || 
		packet.type == Packet.Type.LockStep) {
		server.dispatch(client, packet.data, packet.type);
		console.log("loss : " + packet.sequence.value);
	}
}

function updateLsenv() {
	var length = lsenv.frame.minus(lsenv.to).value;
	var i = 1;
	for (; i <= length; i++) {
		var frame = lsenv.to.add(new RUShort(i));
		var ok = true;
		for (var id in server.clients) {
			var client = server.clients[id];
			if (!client.frameDatas[frame.value]) {
				ok = false;
				break;
			}
		}
		if (!ok) {
			break;
		}
	}
	lsenv.to = lsenv.to.add(new RUShort(i - 1));
}