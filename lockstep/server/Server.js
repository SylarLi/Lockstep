//# 服务器

var dgram = require('dgram');
var Packet = require('./Packet.js');
var Client = require('./Client.js');
var RUShort = require('./RUShort.js');

var ConnnectTimeout = 1 * 1000;
var MaxConnectTimes = 5;
var BreakTimeout = 5 * 1000;

var RTT = 0;	// 模拟延迟

var ClientIndexSeed = 0;

function Server() {

	// ------------------- private method ---------------------- //

	var onListening = function() {
		var address = server.address();
		console.log('UDP Listening On : ' + address.address + ' : ' + address.port);
	};

	var onReceived = function(buffer, remote) {
		setTimeout(function() {
			var packet = new Packet();
			packet.parse(buffer);
			if (!clients[packet.id]) {
				var client = new Client(packet.id);
				client.index = ClientIndexSeed++;
				client.address = remote.address;
				client.port = remote.port;
				clients[packet.id] = client;
				console.log('\nNew client\n' + client.toString());
			}
			var client = clients[packet.id];
			onAck(client, packet);
		}, RTT);
	};

	var onAck = function(client, packet) {
		// 遍历处理超过N次收包未被应答的包
		var lost = [];
		for (var seq in client.quantics) {
			if (packet.ack.laterThan(new RUShort(seq)) && (packet.ack.minus(new RUShort(seq))).value >= 32) {
				lost.push(seq);
			}
		}
		for (var seq in lost) {
			var p = client.quantics[seq];
			delete client.quantics[seq];
			onPacketLoss(client, p);
		}
		// 对已发包列表应用应答，移除已被应答的包
	    for (var i = 0; i < 32; i++) {
	        if ((packet.ackbit & (1 << i)) > 0) {
	        	var seq = packet.ack.minus(new RUShort(i)).value;
	        	if (client.quantics[seq]) {
	        		var p = client.quantics[seq];
					delete client.quantics[seq];
	        		onPacketArrived(client, p);
	        	}
	        }
	    }
	    // 写收包应答
	    if (packet.sequence.laterThan(client.ack)) {
	        client.ackbit = client.ackbit << (packet.sequence.minus(client.ack)).value;
	        client.ackbit |= (1 << 0);
	        client.ack = packet.sequence;
	        onPacketReceived(client, packet);
	    }
	    else {
	    	var value = 1 << (client.ack.minus(packet.sequence)).value;
	    	if ((client.ackbit & value) == 0) {
	    		client.ackbit |= value;
	    		onPacketReceived(client, packet);
	    	}
	        else {
	        	console.log("Receive packet already handle : sequence " + packet.sequence);
	        }
	    }
	}

	// 收到客户端发送的包
	var onPacketReceived = function(client, packet) {
		if (client.netState == Client.NetState.None) {
			if (packet.type == Packet.Type.Connect) {
				client.setConnecting();
			}
		}
		if (client.netState == Client.NetState.Connected) {
			client.lastReceviedTime = Date.now();
			if (packet.type == Packet.Type.HeartBeat) {
				that.dispatch(client, null, packet.type);
			}
			if (that.onPacketReceived) {
				that.onPacketReceived(client, packet);
			}
		}
	}

	// 确定服务器发送的包抵达客户端
	var onPacketArrived = function(client, packet) {
		if (client.netState == Client.NetState.Connecting &&
			packet.type == Packet.Type.Connect) {
			client.setConnected();
		}
		if (client.netState == Client.NetState.Connected) {
			if (that.onPacketArrived) {
				that.onPacketArrived(client, packet);
			}
		}
	}

	// 确认服务器发送的包丢失
	var onPacketLoss = function(client, packet) {
		console.log('Client ' + client.id + ' loss packet ' + packet.type + ' ' + packet.sequence.value);
	    if (client.netState == Client.NetState.Connected) {
			if (that.onPacketLoss) {
				that.onPacketLoss(client, packet);
			}
		}
	}

	// ------------------- member varibles ---------------------- //

	var that = this;

	this.onPacketReceived = null;
	this.onPacketArrived = null;
	this.onPacketLoss = null;
	var clients = this.clients = {};

	var server = this.server = dgram.createSocket('udp4');
	server.on('listening', onListening);
	server.on('message', onReceived);
}

Server.prototype.bind = function(host, port) {
	this.server.bind(port, host);
};

Server.prototype.dispatch = function(client, data, type) {
	if (client) {
		var packet = new Packet();
		packet.type = type;
		packet.data = data;
		packet.id = client.id;
		packet.sequence = client.sequence.add$$();
		packet.ack = client.ack;
		packet.ackbit = client.ackbit;
		client.quantics[packet.sequence.value] = packet;
		var buffer = packet.toBuffer();
		if (buffer && buffer.length > 0) {
			this.server.send(buffer, 0, buffer.length, client.port, client.address);
		}
	}
};

Server.prototype.dispatchAll = function(data, type) {
	for (var id in this.clients) {
		var client = this.clients[id];
		this.dispatch(client, data, type);
	}
};

Server.prototype.update = function(deltaTime) {
	for (var id in this.clients) {
		var client = this.clients[id];
		if (client.netState == Client.NetState.Connecting) {
			if (Date.now() - client.lastConnectTime >= ConnnectTimeout) {
                if (client.connectTimes < MaxConnectTimes) {
                    client.connectTimes++;
                    client.lastConnectTime = Date.now();
                    this.dispatch(client, null, Packet.Type.Connect);
                }
                else {
                    client.setDisconnected();
                }
            }
		}
		else if (client.netState == Client.NetState.Connected) {
			if (Date.now() - client.lastReceviedTime > BreakTimeout) {
				client.setDisconnected();
			}
		}
	}
};

module.exports = Server;