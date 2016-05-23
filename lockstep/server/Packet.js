//# 网络包封装

require("buffer");
var RUShort = require("./RUShort.js");

var Type = {
	None : 0,
	Connect : 1,
	HeartBeat : 2,
	Message : 3,
	LockStep : 4,
}

function Packet() {
	this.type = Type.None;					// ubyte
	this.id = 0;							// uint
	this.sequence = new RUShort(0);			// RUShort
	this.ack = new RUShort(0);				// RUShort
	this.ackbit = 0;						// uint
	this.data = null;						// Buffer
}

Packet.prototype.parse = function(buffer) {
	var index = 0;
	this.type = buffer.readUInt8(index, true);
	index += 1;
	this.id = buffer.readUInt32LE(index, true);
	index += 4;
	this.sequence.value = buffer.readUInt8(index, true);
	index += 1;
	this.ack.value = buffer.readUInt8(index, true);
	index += 1;
	this.ackbit = buffer.readUInt32LE(index, true);
	index += 4;
	if (buffer.length > index) {
		this.data = new Buffer(buffer.length - index);
		buffer.copy(this.data, 0, index, buffer.length);
	}
}

Packet.prototype.toBuffer = function() {
	var buffer = new Buffer(13 + (this.data ? this.data.length : 0));
	var index = 0;
	buffer.writeUInt8(this.type, index, true);
	index += 1;
	buffer.writeUInt32LE(this.id, index, true);
	index += 4;
	buffer.writeUInt8(this.sequence.value, index, true);
	index += 1;
	buffer.writeUInt8(this.ack.value, index, true);
	index += 1;
	buffer.writeUInt32LE(this.ackbit, index, true);
	index += 4;
	if (this.data && this.data.length > 0) {
		this.data.copy(buffer, index, 0, this.data.length);
	}
	return buffer;
}

module.exports = Packet;
module.exports.Type = Type;