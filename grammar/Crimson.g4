grammar Crimson;

compilationUnit
    : declaration* EOF
    ;

declaration
    : namespaceDeclaration
    | interfaceDeclaration
    | enumDeclaration
    | constantDeclaration
    ;

namespaceDeclaration
    : annotation* NAMESPACE qualifiedName namespaceBody
    ;

namespaceBody
    : SEMI
    | LBRACE declaration* RBRACE
    ;

interfaceDeclaration
    : annotation* ABSTRACT? INTERFACE Identifier interfaceBases? interfaceBody
    ;

interfaceBases
    : COLON typeReference (COMMA typeReference)*
    ;

interfaceBody
    : SEMI
    | LBRACE interfaceItem* RBRACE
    ;

interfaceItem
    : interfaceMember
    | interfaceDeclaration
    | enumDeclaration
    ;

interfaceMember
    : constantMember
    | valueMember
    | methodMember
    ;

constantDeclaration
    : annotation* CONST typeReference Identifier (ASSIGN literal)? SEMI
    ;

constantMember
    : annotation* CONST typeReference Identifier (ASSIGN literal)? SEMI
    ;

valueMember
    : annotation* memberModifier* typeReference Identifier (ASSIGN literal)? SEMI
    ;

memberModifier
    : READONLY
    | INTERNAL
    ;

methodMember
    : annotation* typeReferenceOrVoid Identifier LPAREN parameterList? RPAREN SEMI
    ;

parameterList
    : parameter (COMMA parameter)*
    ;

parameter
    : annotation* typeReference Identifier (ASSIGN literal)?
    ;

enumDeclaration
    : annotation* ENUM Identifier enumAssociatedType? enumBody
    ;

enumAssociatedType
    : COLON typeReference
    ;

enumBody
    : SEMI
    | LBRACE enumMemberList? COMMA? RBRACE
    ;

enumMemberList
    : enumMember (COMMA enumMember)*
    ;

enumMember
    : annotation* Identifier (ASSIGN literal)?
    ;

annotation
    : AT qualifiedName annotationArguments?
    ;

annotationArguments
    : LPAREN annotationArgumentList? RPAREN
    ;

annotationArgumentList
    : annotationArgument (COMMA annotationArgument)*
    ;

annotationArgument
    : Identifier ASSIGN literal
    | literal
    ;

typeReferenceOrVoid
    : VOID
    | typeReference
    ;

typeReference
    : typePrimary arraySuffix* nullableSuffix?
    ;

typePrimary
    : primitiveType
    | collectionType
    | qualifiedName
    ;

collectionType
    : LIST LT typeReference GT
    | SET LT typeReference GT
    | MAP LT typeReference COMMA typeReference GT
    ;

arraySuffix
    : LBRACK IntegerLiteral? RBRACK
    ;

nullableSuffix
    : QUESTION
    ;

qualifiedName
    : DOT? Identifier (DOT Identifier)*
    ;

primitiveType
    : BOOL
    | STRING
    | INT8
    | UINT8
    | INT16
    | UINT16
    | INT32
    | UINT32
    | INT64
    | UINT64
    | FLOAT32
    | FLOAT64
    ;

literal
    : FloatLiteral
    | IntegerLiteral
    | StringLiteral
    | TRUE
    | FALSE
    ;

ABSTRACT: 'abstract';
NAMESPACE: 'namespace';
INTERFACE: 'interface';
ENUM: 'enum';
CONST: 'const';
READONLY: 'readonly';
INTERNAL: 'internal';
VOID: 'void';
LIST: 'list';
MAP: 'map';
SET: 'set';
BOOL: 'bool';
STRING: 'string';
INT8: 'int8';
UINT8: 'uint8';
INT16: 'int16';
UINT16: 'uint16';
INT32: 'int32';
UINT32: 'uint32';
INT64: 'int64';
UINT64: 'uint64';
FLOAT32: 'float32';
FLOAT64: 'float64';
TRUE: 'true';
FALSE: 'false';

AT: '@';
COLON: ':';
SEMI: ';';
COMMA: ',';
QUESTION: '?';
ASSIGN: '=';
DOT: '.';
LT: '<';
GT: '>';
LPAREN: '(';
RPAREN: ')';
LBRACE: '{';
RBRACE: '}';
LBRACK: '[';
RBRACK: ']';

FloatLiteral
    : '-'? [0-9]+ '.' [0-9]+
    ;

IntegerLiteral
    : '-'? [0-9]+
    ;

StringLiteral
    : '"' ( '\\' . | ~["\\] )* '"'
    ;

Identifier
    : [A-Za-z_] [A-Za-z0-9_]*
    ;

DOC_LINE_COMMENT
    : '///' ~[\r\n]* -> channel(HIDDEN)
    ;

DOC_BLOCK_COMMENT
    : '/**' .*? '*/' -> channel(HIDDEN)
    ;

LINE_COMMENT
    : '//' ~[\r\n]* -> channel(HIDDEN)
    ;

BLOCK_COMMENT
    : '/*' .*? '*/' -> channel(HIDDEN)
    ;

WS
    : [ \t\r\n]+ -> channel(HIDDEN)
    ;
