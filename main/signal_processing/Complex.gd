class_name Complex
extends RefCounted

# Complex number code modified from https://github.com/tavurth/godot-fft

var re: float = 0.0
var im: float = 0.0

@warning_ignore("shadowed_variable")
func _init(re: float, im: float = 0.0) -> void:
	self.re = re
	self.im = im

func add(other: Complex) -> Complex:
	var dst: Complex = Complex.new(0.0, 0.0)
	dst.re = self.re + other.re
	dst.im = self.im + other.im
	return dst


func sub(other: Complex) -> Complex:
	var dst: Complex = Complex.new(0.0, 0.0)
	dst.re = self.re - other.re
	dst.im = self.im - other.im
	return dst

func mul(other: Complex) -> Complex:
	var dst: Complex = Complex.new(0.0, 0.0)
	dst.re = self.re * other.re - self.im * other.im
	dst.im = self.re * other.im + self.im * other.re
	return dst

func cexp(dst: Complex) -> Complex:
	var er: float = exp(self.re)
	dst.re = er * cos(self.im)
	dst.im = er * sin(self.im)
	return dst
