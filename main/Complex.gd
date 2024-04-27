extends RefCounted
class_name Complex

var re: float = 0.0
var im: float = 0.0

func _init(re: float, im: float = 0.0):
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

func mul(other) -> Complex:
	var dst: Complex = Complex.new(0.0, 0.0)
	dst.re = self.re * other.re - self.im * other.im
	dst.im = self.re * other.im + self.im * other.re
	return dst

func cexp(dst):
	var er = exp(self.re)
	dst.re = er * cos(self.im)
	dst.im = er * sin(self.im)
	return dst

func log():
	# although 'It's just a matter of separating out the real and imaginary parts of jw.' is not a helpful quote
	# the actual formula I found here and the rest was just fiddling / testing and comparing with correct results.
	# http://cboard.cprogramming.com/c-programming/89116-how-implement-complex-exponential-funcs-c.html#post637921:
	if not self.re:
		prints(self.im, "j")
	elif self.im < 0:
		prints(self.re, self.im, "j")
	else:
		print(self.re, "+", self.im, "j")
