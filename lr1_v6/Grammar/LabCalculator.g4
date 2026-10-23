grammar LabCalculator;

compileUnit
    : expression EOF
    ;

expression
    : LPAREN expression RPAREN                                          # ParenthesizedExpr
    | operatorToken=(ADD | SUBTRACT) expression                         # UnaryExpr
    | <assoc=right> expression EXPONENT expression                      # ExponentialExpr
    | expression operatorToken=(MULTIPLY | DIVIDE | DIV | MOD) expression # MultiplicativeExpr
    | expression operatorToken=(ADD | SUBTRACT) expression              # AdditiveExpr
    | MAX LPAREN expression (COMMA | SEMICOLON) expression RPAREN       # MaxExpr
    | MIN LPAREN expression (COMMA | SEMICOLON) expression RPAREN       # MinExpr
    | NUMBER                                                            # NumberExpr
    | IDENTIFIER                                                        # IdentifierExpr
    ;

MAX        : [mM][aA][xX] ;
MIN        : [mM][iI][nN] ;
MOD        : [mM][oO][dD] ;
DIV        : [dD][iI][vV] ;

NUMBER     : [0-9]+ ('.' [0-9]+)? ;
IDENTIFIER : [a-zA-Z]+ [0-9]+ ;

EXPONENT   : '^' ;
MULTIPLY   : '*' ;
DIVIDE     : '/' ;
SUBTRACT   : '-' ;
ADD        : '+' ;
LPAREN     : '(' ;
RPAREN     : ')' ;
COMMA      : ',' ;
SEMICOLON  : ';' ;

WS         : [ \t\r\n]+ -> channel(HIDDEN) ;
